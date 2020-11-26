const axios = require('axios');
const cheerio = require('cheerio');
const robotsParser = require('robots-parser');

axios.defaults.timeout = 15000;

axios.interceptors.response.use(response => response, async error => {
    // Do something with response error
    if(error.code === 'ETIMEOUT' || error.code === 'ECONNRESET' || error.code === 'ECONNABORTED' || error.response?.status > 502 || error.response?.status === 429) {
        // console.log('retrying for url: ' + error.config.url);
        if(error.config.currentRetryAttempt === 3) {
            console.log('Error code: ' + error.code + '     httpCode: ' + error?.response?.status + '     url: ' + error.config.url)
            return Promise.reject(error);
        }
        await new Promise(resolve => setTimeout(resolve, 2000));
        if(error.config.currentRetryAttempt) {
            error.config.currentRetryAttempt++;
        } else {
            error.config.currentRetryAttempt = 1;
        }
        return axios.request(error.config);
    } else {
        return Promise.reject(error);
    }
});

class Axios {
    waitingForSlot = [];
    visitedLinks = new Set();
    robotsTxTCache = new Map();
    visitedLinkCount = 0;

    constructor(parallelRequests) {
        this.parallelRequests = parallelRequests;
        this.availableSlots = this.parallelRequests;
    }

    async request(url) {
        if(this.visitedLinks.has(url)) {
            return null;
        }
        
        this.visitedLinks.add(url);

        const robotsTxTUrl = `${new URL(url).origin}/robots.txt`;
        let robotsTxTContent;
        if(this.robotsTxTCache.has(robotsTxTUrl)) {
            robotsTxTContent = this.robotsTxTCache.get(robotsTxTUrl);
        } else {
            robotsTxTContent = (await axios.get(robotsTxTUrl).catch(err => {
                if(err.response?.status === 404) {
                    return { data: "User-agent: *\nAllow: *\n" };
                } else {
                    // console.log('Errored on ROBOTS.TXT with URL: ' + robotsTxTUrl + 'and code: ' + err.response?.status);
                    if(!err.response) {
                        // console.log('ROBOTS: Critical error. Site ' + robotsTxTUrl + ' not accessible. Code: ' + err.code);
                    }
                    return { data: "User-agent: *\nDisallow: *\n" }
                }
            })).data;
            this.robotsTxTCache.set(robotsTxTUrl, robotsTxTContent);
        }
        
        try {
            if(robotsParser(robotsTxTUrl, robotsTxTContent).isDisallowed(url) ?? false) {
                this.visitedLinkCount++;
                return null;
            }
        } catch(err) {
            console.log('MALFORMED robots.txt on: ' + robotsTxTContent);
            this.visitedLinkCount++;
            return null;
        }

        await this._waitForSlot();
        
        const headRes = await axios.head(url).catch(err => {
            if(err.response?.status) {
                // console.log(`Errored with code: ${err.response.status} at url ${url}`)
            } else {
                // console.log('HEAD: Critical error. Site ' + url + ' not accessible. Code: ' + err.code);
            }
        })
        if (headRes?.headers['content-type']?.indexOf('text/html') === -1 || !headRes?.headers['content-type']) {
            // console.log('Skipping because content is not compatible: ' + headRes?.headers['content-type']);
            if (this.waitingForSlot.length > 0) {
                this.waitingForSlot.shift().resolve();
            } else {
                this.availableSlots++;
            }
            this.visitedLinkCount++;
            return null;
        }
        const res = await axios.get(url, {
            headers: {
                accept: 'text/html'
            }
        }).catch(err => {
            if(err.response?.status) {
                // console.log(`Errored with code: ${err.response.status} at url ${url}`);
                // if(err.response.status >= 500) {
                //     console.log(err.response.data);
                // }
            } else {
                // console.log('GET Critical error. Site ' + url + ' not accessible. Code: ' + err.code);
            }
        })

        if (this.waitingForSlot.length > 0) {
            this.waitingForSlot.shift().resolve();
        } else {
            this.availableSlots++;
        }

        if(!res) {
            this.visitedLinkCount++;
            return null;
        }
        
        const $ = cheerio.load(res.data);
        
        let links = [];
        $('body').find('a').each((i, link) => links.push($(link).attr('href')));

        let pageInfo = {
            title: ($('title')?.text() ?? '').trim(),
            url: res.config.url,
            text: ($('body')?.text() ?? '')
                .trim()
                .toLowerCase()
                .replace(/[\n,.?!@#$%^&*()-=/*+<>|_`~]/g, '')
                .replace(/\s+/g, ' '),
            links: links.map(link => {
                let parsedURL;
                try {
                    parsedURL = new URL(link, url); // Handle malformed URLs. HTML developers are sometimes not careful
                } catch (err) { }
                if(!parsedURL || parsedURL.protocol !== 'https:' && parsedURL.protocol !== 'http:') {
                    return null;
                }
                return parsedURL.href;
            }).filter(x => x !== null)
        };
        this.visitedLinkCount++;
        return pageInfo;
    }

    _waitForSlot() {
        if (this.availableSlots > 0) {
            this.availableSlots--;;
            return Promise.resolve();
        }
        let res;
        this.waitingForSlot.push(new Promise(resolve => {
            res = resolve;
        }));
        this.waitingForSlot[this.waitingForSlot.length - 1].resolve = res;
        return this.waitingForSlot[this.waitingForSlot.length - 1];
    }
}

module.exports = { Axios }