const { Axios } = require('./axios');
const nativeAxios = require('axios');

if(process.argv.length !== 6) {
    console.log(`Invalid amount of parameters.
Please use 4 parameters like this:
node crawler.js "starting url" "number of pages to crawl" "keep already crawled pages" "concurrent requests"`);
    process.exit(1);
}

const pagesToCrawl = parseInt(process.argv[3]);
const keepProgress = process.argv[4] !== "0";
const parallelRequests = parseInt(process.argv[5]);
new URL(process.argv[2]); // Used to exit if URL is invalid

console.log(`Beginning to crawl ${pagesToCrawl} pages starting from ${process.argv[2]}`);
console.log(`Using ${parallelRequests} parallel requests`);
console.log('');

const queue = [process.argv[2]];
const axios = new Axios(parallelRequests);

const visitedLinks = new Set();


const axiosInstance = nativeAxios.create({
    timeout: 60000,
    maxContentLength: Infinity,
    maxBodyLength: Infinity,
});


async function crawl() {

    if(!keepProgress) {
        console.log("Wiping index");
        try {
            await axiosInstance.delete('http://localhost:5000/documents?code=secret_code');
        } catch(err) {
            console.log('Index server error. Exiting...');
            process.exit(1);
        }
    }

    const intervalOfVisitedLinks = setInterval(() => {
        console.log("Visited: " + axios.visitedLinkCount);
        console.log("Waiting in queue: " +  queue.length);
    }, 5000);


    let pendingDocumentsToPushToIndex = [];
    const requests = [];

    while(visitedLinks.size !== pagesToCrawl) {
        if(queue.length === 0) {
            await new Promise(resolve => setTimeout(resolve, 1000)); // wait 1 second
            if(queue.length === 0 && axios.availableSlots === axios.parallelRequests) { // if queue empty and no requests executing
                console.log("Queue is empty.");
                break; // Exit application
            }
            continue;
        }
        const url = queue.shift();
        visitedLinks.add(url);
        const request = axios.request(url);
        requests.push(request)
        request.then(data => {
            if(data) {
                if(data.text && data.title) {
                    pendingDocumentsToPushToIndex.push({
                        title: data.title,
                        url: data.url,
                        text: data.text
                    });
                    if (pendingDocumentsToPushToIndex.length === 2000 && queue.length) { // If it's ready to exit do not store documents
                        storeDocumentsToIndex(pendingDocumentsToPushToIndex);
                        pendingDocumentsToPushToIndex = [];
                    }
                }


                queue.push(...data.links.map(x => {
                    const indexOfHash = x.lastIndexOf('#');
                    if(indexOfHash > -1) {
                        return x.substring(0, indexOfHash); //Filter out the hash part of a URL
                    }
                    return x;
                }).filter(link => !visitedLinks.has(link)));

                requests.splice(requests.indexOf(request), 1);
            }
        });

    }
    if(pendingDocumentsToPushToIndex.length) {
        await Promise.all(requests);
        storeDocumentsToIndex(pendingDocumentsToPushToIndex);
    }
    console.log('Exiting...');
    clearInterval(intervalOfVisitedLinks); // Clear the console output interval
}

function storeDocumentsToIndex(documents) {
    axiosInstance.post('http://localhost:5000/documents', documents).catch((err) => {
        if(err?.response?.status < 500) {
            // console.log(err.response.data.errors);
        }
        console.log('Index server error. Exiting...');
        process.exit(1);
    });
}

crawl().catch(console.error);