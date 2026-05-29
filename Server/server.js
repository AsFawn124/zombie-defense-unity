#!/usr/bin/env node
/**
 * 僵尸防线 - 游戏服务端 (开发测试用)
 * 轻量级 WebSocket+REST 服务器
 * 
 * 启动: node server.js
 * 端口: 3000 (HTTP) / 3001 (WebSocket)
 * 
 * 功能:
 * - 玩家认证
 * - 竞技场匹配
 * - 合作房间管理
 * - 公会系统
 * - 排行榜
 * - 赛季数据
 */

const http = require('http');
const WebSocket = require('ws');
const crypto = require('crypto');

// ============ 配置 ============
const HTTP_PORT = 3000;
const WS_PORT = 3001;
const JWT_SECRET = 'zombie-defense-secret-key-change-in-production';

// ============ 内存数据库 ============
const DB = {
    players: new Map(),          // playerId -> Player
    sessions: new Map(),         // ws key -> playerId
    arenaQueue: [],             // 匹配队列
    activeBattles: new Map(),   // battleId -> Battle
    coopRooms: new Map(),       // roomId -> CoopRoom
    guilds: new Map(),          // guildId -> Guild
    leaderboards: {
        arena: [],
        guild: [],
        season: [],
        wave: [],
    }
};

// ============ 玩家管理 ============
class Player {
    constructor(id, name) {
        this.playerId = id || `player_${crypto.randomBytes(6).toString('hex')}`;
        this.playerName = name || 'Guest';
        this.elo = 1200;
        this.rank = 0;
        this.guildId = null;
        this.seasonData = { battlePassLevel: 0, battlePassTier: 0, score: 0 };
        this.highestWave = 0;
        this.totalWins = 0;
        this.createdAt = Date.now();
        this.lastLogin = Date.now();
    }

    toJSON() {
        return {
            playerId: this.playerId,
            playerName: this.playerName,
            elo: this.elo,
            rank: this.rank,
            guildId: this.guildId,
            highestWave: this.highestWave,
            totalWins: this.totalWins,
        };
    }
}

// ============ 匹配系统 (Elo算法) ============
class Matchmaker {
    static findMatch(player, queue) {
        // 移除已经在队列中的
        queue = queue.filter(p => p.playerId !== player.playerId);

        // 按Elo差距排序，优先匹配最近似的对手
        let bestMatch = null;
        let bestDiff = Infinity;

        for (const opponent of queue) {
            const diff = Math.abs(player.elo - opponent.elo);
            if (diff < bestDiff) {
                bestDiff = diff;
                bestMatch = opponent;
            }
        }

        // Elo差距不超过300可以匹配
        if (bestMatch && bestDiff <= 300) {
            return bestMatch;
        }

        return null; // 无合适对手
    }

    static calculateElo(winnerElo, loserElo, K = 32) {
        const expectedWinner = 1 / (1 + Math.pow(10, (loserElo - winnerElo) / 400));
        const expectedLoser = 1 - expectedWinner;

        const newWinnerElo = Math.round(winnerElo + K * (1 - expectedWinner));
        const newLoserElo = Math.round(loserElo + K * (0 - expectedLoser));

        return { newWinnerElo, newLoserElo, winnerChange: newWinnerElo - winnerElo, loserChange: newLoserElo - loserElo };
    }
}

// ============ WebSocket 服务器 ============
const wss = new WebSocket.Server({ port: WS_PORT });
console.log(`[Server] WebSocket 服务启动在端口 ${WS_PORT}`);

wss.on('connection', (ws) => {
    const sessionId = crypto.randomBytes(8).toString('hex');
    DB.sessions.set(sessionId, { ws, playerId: null });

    console.log(`[WS] 新连接: ${sessionId}`);

    ws.on('message', (data) => {
        try {
            const envelope = JSON.parse(data.toString());
            handleMessage(ws, sessionId, envelope);
        } catch (e) {
            sendError(ws, 400, 'Invalid message format');
        }
    });

    ws.on('close', () => {
        const session = DB.sessions.get(sessionId);
        if (session && session.playerId) {
            // 从匹配队列中移除
            DB.arenaQueue = DB.arenaQueue.filter(p => p.playerId !== session.playerId);
            console.log(`[WS] 玩家离线: ${session.playerId}`);
        }
        DB.sessions.delete(sessionId);
    });

    // 心跳
    ws.isAlive = true;
    ws.on('pong', () => { ws.isAlive = true; });
});

// 心跳检测
const heartbeatInterval = setInterval(() => {
    wss.clients.forEach((ws) => {
        if (ws.isAlive === false) return ws.terminate();
        ws.isAlive = false;
        ws.ping();
    });
}, 30000);

wss.on('close', () => clearInterval(heartbeatInterval));

// ============ 消息路由 ============
function handleMessage(ws, sessionId, envelope) {
    const { type, payload } = envelope;

    try {
        const data = JSON.parse(payload);

        switch (type) {
            // 认证
            case 'auth':
                handleAuth(ws, sessionId, data);
                break;
            case 'heartbeat':
                sendMessage(ws, 'heartbeat_ack', { serverTime: Date.now() });
                break;

            // 竞技场
            case 'arena_match':
                handleArenaMatch(ws, sessionId, data);
                break;
            case 'arena_submit':
                handleArenaSubmit(ws, sessionId, data);
                break;

            // 合作模式
            case 'coop_create':
                handleCoopCreate(ws, sessionId, data);
                break;
            case 'coop_join':
                handleCoopJoin(ws, sessionId, data);
                break;
            case 'coop_leave':
                handleCoopLeave(ws, sessionId, data);
                break;
            case 'coop_action':
                handleCoopAction(ws, sessionId, data);
                break;

            // 公会
            case 'guild_create':
                handleGuildCreate(ws, sessionId, data);
                break;
            case 'guild_join':
                handleGuildJoin(ws, sessionId, data);
                break;
            case 'guild_war_start':
                handleGuildWar(ws, sessionId, data);
                break;

            // 排行榜
            case 'leaderboard_fetch':
                handleLeaderboardFetch(ws, sessionId, data);
                break;

            // 赛季
            case 'season_claim_pass':
                handleSeasonClaimPass(ws, sessionId, data);
                break;

            default:
                sendMessage(ws, 'error', { code: 404, message: `Unknown message type: ${type}` });
        }
    } catch (e) {
        sendError(ws, 500, `Message handling error: ${e.message}`);
    }
}

// ============ 消息处理 ============

function handleAuth(ws, sessionId, data) {
    let player = DB.players.get(data.playerId);

    if (!player) {
        player = new Player(data.playerId, data.playerName || 'Player');
        DB.players.set(player.playerId, player);
    }

    player.lastLogin = Date.now();
    DB.sessions.get(sessionId).playerId = player.playerId;

    sendMessage(ws, 'auth_response', {
        success: true,
        playerId: player.playerId,
        playerName: player.playerName,
        serverTime: Date.now(),
    });
}

function handleArenaMatch(ws, sessionId, data) {
    const player = getPlayer(sessionId);
    if (!player) return sendError(ws, 401, 'Not authenticated');

    const opponent = Matchmaker.findMatch(player, DB.arenaQueue);

    if (opponent) {
        // 找到对手，创建战斗
        DB.arenaQueue = DB.arenaQueue.filter(p => p.playerId !== opponent.playerId);
        const battleId = `battle_${crypto.randomBytes(6).toString('hex')}`;

        DB.activeBattles.set(battleId, {
            battleId,
            players: [player.playerId, opponent.playerId],
            startTime: Date.now(),
        });

        // 通知双方
        sendMessage(ws, 'match_found', {
            battleId,
            opponentId: opponent.playerId,
            opponentName: opponent.playerName,
            opponentRank: opponent.rank,
            opponentElo: opponent.elo,
            matchTime: Date.now(),
        });

        const opponentWs = getPlayerWs(opponent.playerId);
        if (opponentWs) {
            sendMessage(opponentWs, 'match_found', {
                battleId,
                opponentId: player.playerId,
                opponentName: player.playerName,
                opponentRank: player.rank,
                opponentElo: player.elo,
                matchTime: Date.now(),
            });
        }
    } else {
        // 加入匹配队列
        DB.arenaQueue.push(player);
        sendMessage(ws, 'arena_queue_status', {
            queuePosition: DB.arenaQueue.length,
            estimatedWait: DB.arenaQueue.length * 5, // 估算等待秒数
        });
    }
}

function handleArenaSubmit(ws, sessionId, data) {
    const player = getPlayer(sessionId);
    if (!player) return sendError(ws, 401, 'Not authenticated');

    const battle = DB.activeBattles.get(data.battleId);
    if (!battle) return sendError(ws, 404, 'Battle not found');

    const opponentId = battle.players.find(id => id !== player.playerId);
    const opponent = DB.players.get(opponentId);

    // Elo计算
    let eloResult;
    if (data.isWin) {
        eloResult = Matchmaker.calculateElo(player.elo, opponent.elo);
        player.elo = eloResult.newWinnerElo;
        opponent.elo = eloResult.newLoserElo;
        player.totalWins++;
    } else {
        eloResult = Matchmaker.calculateElo(opponent.elo, player.elo);
        opponent.elo = eloResult.newWinnerElo;
        player.elo = eloResult.newLoserElo;
        opponent.totalWins++;
    }

    DB.activeBattles.delete(data.battleId);

    sendMessage(ws, 'arena_battle_result', {
        battleId: data.battleId,
        winnerId: data.isWin ? player.playerId : opponentId,
        eloChange: data.isWin ? eloResult.winnerChange : eloResult.loserChange,
        newRank: calculateRank(player.elo),
        rewards: ['gold_500', 'arena_token_10'],
    });

    // 更新排行榜
    updateLeaderboard('arena', player);
}

function handleCoopCreate(ws, sessionId, data) {
    const player = getPlayer(sessionId);
    if (!player) return sendError(ws, 401, 'Not authenticated');

    const roomId = `room_${crypto.randomBytes(4).toString('hex')}`;
    const room = {
        roomId,
        roomName: data.roomName || `${player.playerName}'s Room`,
        hostId: player.playerId,
        password: data.password || '',
        maxPlayers: Math.min(data.maxPlayers || 4, 4),
        players: [{
            playerId: player.playerId,
            playerName: player.playerName,
            isReady: true,
            isHost: true,
            towersBuilt: 0,
            enemiesKilled: 0,
        }],
        state: 'waiting',
        currentWave: 0,
        maxWave: 100,
    };

    DB.coopRooms.set(roomId, room);

    sendMessage(ws, 'coop_room_update', room);
}

function handleCoopJoin(ws, sessionId, data) {
    const player = getPlayer(sessionId);
    if (!player) return sendError(ws, 401, 'Not authenticated');

    const room = DB.coopRooms.get(data.roomId);
    if (!room) return sendError(ws, 404, 'Room not found');
    if (room.password && room.password !== data.password) return sendError(ws, 403, 'Wrong password');
    if (room.players.length >= room.maxPlayers) return sendError(ws, 400, 'Room full');
    if (room.state !== 'waiting') return sendError(ws, 400, 'Game already started');

    room.players.push({
        playerId: player.playerId,
        playerName: player.playerName,
        isReady: false,
        isHost: false,
        towersBuilt: 0,
        enemiesKilled: 0,
    });

    // 广播给房间所有人
    broadcastToRoom(room.roomId, 'coop_room_update', room);
}

function handleCoopLeave(ws, sessionId, data) {
    const player = getPlayer(sessionId);
    if (!player) return;

    const room = DB.coopRooms.get(data.roomId);
    if (!room) return;

    room.players = room.players.filter(p => p.playerId !== player.playerId);

    if (room.players.length === 0) {
        DB.coopRooms.delete(data.roomId);
    } else {
        broadcastToRoom(room.roomId, 'coop_room_update', room);
    }
}

function handleCoopAction(ws, sessionId, data) {
    const player = getPlayer(sessionId);
    if (!player) return;

    const room = DB.coopRooms.get(data.roomId);
    if (!room) return;

    // 广播动作给房间其他人
    broadcastToRoom(data.roomId, 'coop_action', {
        playerId: player.playerId,
        actionType: data.actionType,
        actionData: data.actionData,
    }, player.playerId);
}

function handleGuildCreate(ws, sessionId, data) {
    const player = getPlayer(sessionId);
    if (!player) return sendError(ws, 401, 'Not authenticated');
    if (player.guildId) return sendError(ws, 400, 'Already in a guild');

    const guildId = `guild_${crypto.randomBytes(4).toString('hex')}`;
    const guild = {
        guildId,
        guildName: data.guildName,
        tag: data.tag,
        description: data.description,
        leaderId: player.playerId,
        members: [{ playerId: player.playerId, playerName: player.playerName, role: 'leader' }],
        level: 1,
        experience: 0,
        createdAt: Date.now(),
    };

    DB.guilds.set(guildId, guild);
    player.guildId = guildId;

    sendMessage(ws, 'guild_event', {
        eventType: 'guild_created',
        guildId,
        guildName: guild.guildName,
        timestamp: Date.now(),
    });
}

function handleGuildJoin(ws, sessionId, data) {
    const player = getPlayer(sessionId);
    if (!player) return sendError(ws, 401, 'Not authenticated');
    if (player.guildId) return sendError(ws, 400, 'Already in a guild');

    const guild = DB.guilds.get(data.guildId);
    if (!guild) return sendError(ws, 404, 'Guild not found');
    if (guild.members.length >= 50) return sendError(ws, 400, 'Guild full');

    guild.members.push({ playerId: player.playerId, playerName: player.playerName, role: 'member' });
    player.guildId = data.guildId;

    broadcastToGuild(guild.guildId, 'guild_event', {
        eventType: 'member_join',
        guildId: guild.guildId,
        guildName: guild.guildName,
        data: `${player.playerName} joined the guild!`,
        timestamp: Date.now(),
    });
}

function handleGuildWar(ws, sessionId, data) {
    const player = getPlayer(sessionId);
    if (!player) return sendError(ws, 401, 'Not authenticated');

    sendMessage(ws, 'guild_event', {
        eventType: 'war_start',
        guildId: data.guildId,
        data: `War declared against guild ${data.targetGuildId}!`,
        timestamp: Date.now(),
    });
}

function handleLeaderboardFetch(ws, sessionId, data) {
    const board = DB.leaderboards[data.boardType] || [];

    sendMessage(ws, 'leaderboard_update', {
        boardType: data.boardType,
        entries: board.slice(data.offset || 0, (data.offset || 0) + (data.limit || 50)),
        playerRank: getPlayerRank(data.boardType, data.playerId),
        totalEntries: board.length,
    });
}

function handleSeasonClaimPass(ws, sessionId, data) {
    const player = getPlayer(sessionId);
    if (!player) return sendError(ws, 401, 'Not authenticated');

    sendMessage(ws, 'season_pass_claimed', {
        passLevel: data.passLevel,
        rewards: ['gold_200', 'chip_random_1'],
    });
}

// ============ 工具函数 ============

function getPlayer(sessionId) {
    const session = DB.sessions.get(sessionId);
    if (!session || !session.playerId) return null;
    return DB.players.get(session.playerId);
}

function getPlayerWs(playerId) {
    for (const [sid, session] of DB.sessions) {
        if (session.playerId === playerId && session.ws.readyState === WebSocket.OPEN) {
            return session.ws;
        }
    }
    return null;
}

function sendMessage(ws, type, data) {
    if (ws.readyState === WebSocket.OPEN) {
        ws.send(JSON.stringify({
            type,
            payload: JSON.stringify(data),
            timestamp: Date.now(),
        }));
    }
}

function sendError(ws, code, message) {
    sendMessage(ws, 'error', { code, message });
}

function broadcastToRoom(roomId, type, data, excludePlayerId = null) {
    const room = DB.coopRooms.get(roomId);
    if (!room) return;

    for (const p of room.players) {
        if (p.playerId === excludePlayerId) continue;
        const ws = getPlayerWs(p.playerId);
        if (ws) sendMessage(ws, type, data);
    }
}

function broadcastToGuild(guildId, type, data) {
    const guild = DB.guilds.get(guildId);
    if (!guild) return;

    for (const member of guild.members) {
        const ws = getPlayerWs(member.playerId);
        if (ws) sendMessage(ws, type, data);
    }
}

function calculateRank(elo) {
    if (elo >= 2000) return 1;
    if (elo >= 1800) return 10;
    if (elo >= 1600) return 50;
    if (elo >= 1400) return 100;
    return 500;
}

function updateLeaderboard(boardType, player) {
    const board = DB.leaderboards[boardType];
    const existing = board.findIndex(e => e.playerId === player.playerId);

    const entry = {
        playerId: player.playerId,
        playerName: player.playerName,
        score: player.elo,
    };

    if (existing >= 0) board[existing] = entry;
    else board.push(entry);

    board.sort((a, b) => b.score - a.score);

    // 限制排行榜大小
    if (board.length > 1000) board.length = 1000;
}

function getPlayerRank(boardType, playerId) {
    const board = DB.leaderboards[boardType];
    const index = board.findIndex(e => e.playerId === playerId);
    return index >= 0 ? index + 1 : null;
}

// ============ HTTP API (REST) ============
const httpServer = http.createServer((req, res) => {
    res.setHeader('Content-Type', 'application/json');
    res.setHeader('Access-Control-Allow-Origin', '*');

    // 简易路由
    const url = new URL(req.url, `http://localhost:${HTTP_PORT}`);

    if (req.method === 'GET' && url.pathname === '/api/health') {
        res.end(JSON.stringify({
            status: 'ok',
            uptime: process.uptime(),
            players: DB.players.size,
            activeRooms: DB.coopRooms.size,
            queueSize: DB.arenaQueue.length,
        }));
    } else if (req.method === 'GET' && url.pathname === '/api/leaderboard') {
        const boardType = url.searchParams.get('type') || 'arena';
        res.end(JSON.stringify(DB.leaderboards[boardType] || []));
    } else if (req.method === 'GET' && url.pathname === '/api/player') {
        const playerId = url.searchParams.get('id');
        const player = DB.players.get(playerId);
        if (player) {
            res.end(JSON.stringify(player.toJSON()));
        } else {
            res.statusCode = 404;
            res.end(JSON.stringify({ error: 'Player not found' }));
        }
    } else if (req.method === 'POST' && url.pathname === '/api/config') {
        // 远程配置下发
        let body = '';
        req.on('data', chunk => body += chunk);
        req.on('end', () => {
            res.end(JSON.stringify({
                version: '1.0.0',
                configs: {
                    'battle.base_tower_damage': 100,
                    'economy.gold_per_kill': 10,
                    'drop.equipment_rate': 0.15,
                },
                featureFlags: {
                    'arena_enabled': true,
                    'guild_enabled': true,
                    'coop_enabled': true,
                    'limited_gacha_enabled': false,
                },
                announcements: [{
                    id: 'ann_001',
                    title: 'Welcome!',
                    content: 'Server is running.',
                    priority: 1,
                }],
            }));
        });
    } else {
        res.statusCode = 404;
        res.end(JSON.stringify({ error: 'Not found' }));
    }
});

httpServer.listen(HTTP_PORT, () => {
    console.log(`[Server] HTTP API 启动在端口 ${HTTP_PORT}`);
    console.log(`[Server] 健康检查: http://localhost:${HTTP_PORT}/api/health`);
});

// 优雅关闭
process.on('SIGTERM', () => {
    console.log('[Server] 正在关闭...');
    wss.close();
    httpServer.close();
    process.exit(0);
});
