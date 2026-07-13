import http from 'k6/http';
import { check, sleep } from 'k6';

// A light smoke/soak check, not a stress test - this is meant to run safely
// against a real home LAN deployment on request, not hammer it. The old
// profile here ramped to 200 VUs, which has no place in that context.
export const options = {
    insecureSkipTLSVerify: true,
    vus: 5,
    duration: '30s',
    thresholds: {
        http_req_failed: ['rate<0.01'],
        checks: ['rate>0.99'],
    },
};

const BASE_URL = __ENV.BASE_URL || 'https://localhost';
const testImageBase = new Uint8Array(open('./fixtures/test.png', 'b'));

// Logs in once and creates one scratch album, shared by every VU/iteration.
export function setup() {
    const loginRes = http.post(
        `${BASE_URL}/api/users/login`,
        JSON.stringify({
            Username: __ENV.ADMIN_USERNAME || 'admin',
            Password: __ENV.ADMIN_PASSWORD || 'changeme',
        }),
        { headers: { 'Content-Type': 'application/json' } }
    );
    check(loginRes, { 'setup: login status was 200': (r) => r.status === 200 });

    const token = loginRes.json('token');
    if (!token) {
        throw new Error(
            'Login failed in setup() - cannot continue without a token. ' +
            'Run with -e ADMIN_USERNAME=... -e ADMIN_PASSWORD=...'
        );
    }

    const authHeaders = { headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` } };
    const albumRes = http.post(
        `${BASE_URL}/api/v1/albums`,
        JSON.stringify({ Title: `k6-load-test-${Date.now()}`, Description: 'Created by k6 load_tests.js' }),
        authHeaders
    );
    check(albumRes, { 'setup: album creation status was 201': (r) => r.status === 201 });

    return { token, albumId: albumRes.json('id') };
}

function stringToBytes(str) {
    const bytes = new Uint8Array(str.length);
    for (let i = 0; i < str.length; i++) {
        bytes[i] = str.charCodeAt(i) & 0xff;
    }
    return bytes;
}

// The API rejects a second upload of identical bytes as a duplicate (by
// design - Phase 2's dedup-by-content-hash check). Every VU/iteration needs
// genuinely different bytes or every upload past the first would 409 rather
// than 201. Appending unique trailing bytes changes the hash without
// affecting the magic-byte check, which only inspects the header.
function uniqueImageBytes() {
    const suffix = stringToBytes(`-${__VU}-${__ITER}-${Date.now()}-${Math.random()}`);
    const combined = new Uint8Array(testImageBase.length + suffix.length);
    combined.set(testImageBase, 0);
    combined.set(suffix, testImageBase.length);
    return combined.buffer;
}

export default function (data) {
    const authHeader = { headers: { Authorization: `Bearer ${data.token}` } };

    // List scenario
    const responses = http.batch([
        ['GET', `${BASE_URL}/api/v1/albums`, null, authHeader],
        ['GET', `${BASE_URL}/api/v1/albums/${data.albumId}`, null, authHeader],
        ['GET', `${BASE_URL}/api/photos`, null, authHeader],
    ]);
    check(responses[0], { 'albums list status was 200': (r) => r.status === 200 });
    check(responses[1], { 'album by id status was 200': (r) => r.status === 200 });
    check(responses[2], { 'photos list status was 200': (r) => r.status === 200 });

    // Upload scenario
    const uploadRes = http.post(
        `${BASE_URL}/api/photos/upload`,
        {
            File: http.file(uniqueImageBytes(), `k6-${__VU}-${__ITER}.png`, 'image/png'),
            AlbumId: String(data.albumId),
            Title: `k6 upload VU${__VU} iter${__ITER}`,
        },
        authHeader
    );
    check(uploadRes, { 'upload status was 201': (r) => r.status === 201 });

    sleep(1);
}
