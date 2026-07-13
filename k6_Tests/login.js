import http from 'k6/http';
import { check } from 'k6';

// Points at the Caddy proxy by default, matching the actual deployment
// (the API hasn't been reachable on :3000 directly since Phase 2 dropped the
// hardcoded Kestrel listener). Self-signed cert on a LAN-only deployment, so
// TLS verification is off here rather than needing the local CA imported.
export const options = {
    insecureSkipTLSVerify: true,
};

const BASE_URL = __ENV.BASE_URL || 'https://localhost';

export default function () {
    const url = `${BASE_URL}/api/users/login`;
    // Credentials come from the ADMIN_USERNAME/ADMIN_PASSWORD bootstrap; there is no
    // seeded test user anymore. Run with: k6 run -e ADMIN_USERNAME=... -e ADMIN_PASSWORD=... login.js
    const payload = JSON.stringify({
        Username: __ENV.ADMIN_USERNAME || 'admin',
        Password: __ENV.ADMIN_PASSWORD || 'changeme',
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    const response = http.post(url, payload, params);

    check(response, {
        'login status was 200': (r) => r.status === 200,
    });

    const token = response.json('token');
    console.log(`Bearer ${token}`);
}
