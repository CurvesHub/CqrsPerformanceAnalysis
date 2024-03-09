import http from 'k6/http';
import { sleep, randomSeed } from 'k6';

export const options = {
    stages: [
        /*        { duration: '5s', target: 1 },
                { duration: '5s', target: 2 },
                { duration: '5s', target: 0 }*/
        { duration: '15s', target: 5 },     // 15s
        { duration: '30s', target: 5 },     // 45s
        { duration: '15s', target: 10 },    // 1m
        { duration: '60s', target: 10 },    // 2m
        { duration: '15s', target: 15 },    // 2m15s
        { duration: '120s', target: 15 },   // 4m15s
        { duration: '15s', target: 10 },    // 4m30s
        { duration: '60s', target: 10 },    // 5m30s
        { duration: '15s', target: 5 },     // 5m45s
        { duration: '30s', target: 5 },     // 6m15s
        { duration: '10s', target: 1 },     // 6m25s
        { duration: '5s', target: 0 }       // 6m30s
    ]
};

// Hard-coded seed for determinism
const seed = 'hardcoded_seed';
randomSeed(seed);

// Generate a random integer from 1 to max (inclusive)
function randomInt(max) {
    return Math.floor(Math.random() * max) + 1;
}

const baseUrl = 'http://host.docker.internal:5012/attributes/subAttributes';

function randomUrl() {
    return `${baseUrl}?rootCategoryId=1&articleNumber=${randomInt(10000)}&attributeIds=${randomInt(90000)},${randomInt(90000)},${randomInt(90000)}`;
}

export default function () {
    http.get(randomUrl());
    sleep(1); // Sleep for 1 second
}
