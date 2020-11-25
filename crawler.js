const { Axios } = require('./axios');

const parallelRequests = 32;

const queue = ['https://example.com'];
const axios = new Axios(parallelRequests);


async function crawl() {
    while(true) {
        if(queue.length === 0) {
            // console.log('waiting');
            await new Promise(resolve => setTimeout(resolve, 1000));
            if(queue.length === 0 && axios.availableSlots === axios.parallelRequests) {
                break;
            }
            continue;
        }
        axios.request(queue.shift()).then(data => {
            if(data) {
                queue.push(...data.links);
            }
        });
    }
}

crawl().catch(console.error)