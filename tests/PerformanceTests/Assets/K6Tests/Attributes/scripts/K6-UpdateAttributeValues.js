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

function body() {
    const randomAttributeId = randomInt(10000);
    const randomCharacteristicId = Math.random() > 0.5 ? 0 : 1;
    return JSON.stringify({
        rootCategoryId: 1,
        articleNumber: randomInt(10000).toString(),
        newAttributeValues: [
            {
                attributeId: randomAttributeId,
                innerValues: [
                    {
                        characteristicId: randomCharacteristicId,
                        values: ["true"]
                    }
                ]
            },
            {
                attributeId: randomAttributeId + 30000,
                innerValues: [
                    {
                        characteristicId: randomCharacteristicId,
                        values: ["true"]
                    }
                ]
            },
            {
                attributeId: randomAttributeId + 60000,
                innerValues: [
                    {
                        characteristicId: randomCharacteristicId,
                        values: ["true"]
                    }
                ]
            }
        ]
    });
}

const params = { headers: { 'Content-Type': 'application/json' } };
const baseUrl = `http://host.docker.internal:${__ENV.API_PORT_TO_USE}/attributes`;

export default function () {
    http.put(baseUrl, body(), params);
    sleep(1); // Sleep for 1 second
}
