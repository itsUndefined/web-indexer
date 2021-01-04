const { Axios } = require('./axios');
const nativeAxios = require('axios');

const parallelRequests = 48;

const queue = ['https://www.protothema.gr/'];
const axios = new Axios(parallelRequests);

const visitedLinks = new Set();


const axiosInstance = nativeAxios.create({
    timeout: 60000,
    maxContentLength: Infinity,
    maxBodyLength: Infinity,
});


async function crawl() {
    const intervalOfVisitedLinks = setInterval(() => {
        console.log("Visited: " + axios.visitedLinkCount);
        console.log("Waiting in queue: " + (axios.visitedLinks.size - axios.visitedLinkCount));
    }, 5000);


    let pendingDocumentsToPushToIndex = [];

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
        const url = queue.shift();
        visitedLinks.add(url);
        axios.request(url).then(data => {
            if(data) {
                if(data.text && data.title) {
                    pendingDocumentsToPushToIndex.push({
                        title: data.title,
                        url: data.url,
                        text: data.text
                    });

                    if (pendingDocumentsToPushToIndex.length === 2000) {
                        axiosInstance.post('http://localhost:5000/documents', pendingDocumentsToPushToIndex).catch((err) => {
                            if(err?.response?.status < 500) {
                                // console.log(err.response.data.errors);
                            }
                            console.log('Index server error. Exiting...')
                            process.exit(1)
                        });
                        pendingDocumentsToPushToIndex = [];
                    }
                }
                queue.push(...data.links.map(x => x.substring(0, x.lastIndexOf('#'))).filter(link => !visitedLinks.has(link)));
            }
        });
    }
}

crawl().catch(console.error)