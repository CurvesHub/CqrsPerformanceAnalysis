import http from 'k6/http';
import { sleep } from 'k6';

export const options = {
    stages: [
        { duration: '5s', target: 1 },
        { duration: '5s', target: 2 },
        { duration: '5s', target: 0 }
    ]
};

const baseUrl = `http://host.docker.internal:${__ENV.API_PORT_TO_USE}/rootCategories`;

export default function () {
    http.get(baseUrl);
    sleep(1); // Sleep for 1 second
}
