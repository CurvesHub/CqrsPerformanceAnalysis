import http from 'k6/http';
import { sleep } from 'k6';

export const options = {
    // Test duration: 2min
    // Purpose: Test the performance when the number of users increases, plateau and decreases
    stages: [
        { duration: '30s', target: 15 },    // Ramp-up from 1 to 15 users
        { duration: '1m', target: 15 },     // Stay at 15 users for 1 minute
        { duration: '30s', target: 0 },     // Ramp-down from 10 to 0 users
    ]
};

const baseUrl = `http://host.docker.internal:${__ENV.API_PORT_TO_USE}/rootCategories`;

export default function () {
    http.get(baseUrl);
    sleep(1); // Sleep for 1 second
}
