import http from 'k6/http';
import { check } from 'k6';

export default function () {
    const url = 'http://localhost:3000/api/users/login'; // Replace with your login endpoint
    const payload = JSON.stringify({
        Username: 'giquardo',
        Password: '123',
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