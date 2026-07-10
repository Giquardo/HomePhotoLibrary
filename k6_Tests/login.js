import http from 'k6/http';
import { check } from 'k6';

export default function () {
    const url = 'http://localhost:3000/api/users/login'; // Replace with your login endpoint
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