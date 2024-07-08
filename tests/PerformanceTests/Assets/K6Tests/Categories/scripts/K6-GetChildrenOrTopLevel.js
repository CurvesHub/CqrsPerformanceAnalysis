import http from 'k6/http';
import { sleep, randomSeed } from 'k6';

export const options = {
    stages: [
        { duration: '5s', target: 1 },
        { duration: '5s', target: 2 },
        { duration: '5s', target: 0 }
    ]
};

// Env seed for determinism
const seed = __ENV.MY_SEED;
randomSeed(seed);

// Generate a random integer from 1 to max (inclusive)
function randomInt(max) {
    return Math.floor(Math.random() * max) + 1;
}

const baseUrl = `http://host.docker.internal:${__ENV.API_PORT_TO_USE}/categories/children`;

function randomUrl() { // 1/3 chance of not having a category number (request all base categories)
    return Math.random() > 0.33
        ? `${baseUrl}?rootCategoryId=1&articleNumber=${randomInt(10000)}&categoryNumber=${randomInt(30000)}`
        : `${baseUrl}?rootCategoryId=1&articleNumber=${randomInt(10000)}`;
}

export default function () {
    http.get(randomUrl());
    sleep(1); // Sleep for 1 second
}
