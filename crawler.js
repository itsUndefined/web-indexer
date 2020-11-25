const { Axios } = require('./axios');

const parallelRequests = 64;

const queue = ['http://quiz4math.gr'];
const axios = new Axios(parallelRequests);


async function crawl() {
    const intervalOfVisitedLinks = setInterval(() => {
        console.log("Visited: " + axios.visitedLinkCount);
        console.log("Waiting in queue: " + (axios.visitedLinks.size - axios.visitedLinkCount));
    }, 5000);

    while(true) {
        if(queue.length === 0) {
            await new Promise(resolve => setTimeout(resolve, 1000));
            if(queue.length === 0) {
                if(axios.availableSlots === axios.parallelRequests) {
                    clearInterval(intervalOfVisitedLinks);
                    break;
                }
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