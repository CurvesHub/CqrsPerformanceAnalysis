import http from 'k6/http';
import { sleep, randomSeed } from 'k6';

export const options = {
    // Test duration: 2min
    // Purpose: Test the performance when the number of users increases, plateau and decreases
    stages: [
        { duration: '30s', target: 15 },    // Ramp-up from 1 to 15 users
        { duration: '1m', target: 15 },     // Stay at 15 users for 1 minute
        { duration: '30s', target: 0 },     // Ramp-down from 10 to 0 users
    ]
};

// Env seed for determinism
const seed = __ENV.MY_SEED;
randomSeed(seed);

// Generate a random integer from 1 to max (inclusive)
function randomInt(max) {
    return Math.floor(Math.random() * max) + 1;
}

const baseUrl = `http://host.docker.internal:${__ENV.API_PORT_TO_USE}/categories/search`;

function randomUrl() { // 1/3 chance of not having a category number (request all base categories)
    return Math.random() > 0.33
        ? `${baseUrl}?rootCategoryId=1&articleNumber=${randomInt(10000)}&categoryNumber=${randomInt(30000)}`
        : `${baseUrl}?rootCategoryId=1&articleNumber=${randomInt(10000)}&searchTerm=${randomInt(30000)}`;
}

export default function () {
    http.get(randomUrl());
    sleep(1); // Sleep for 1 second
}
