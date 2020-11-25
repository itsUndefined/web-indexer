const axios = require('axios');
const cheerio = require('cheerio');
const robotsParser = require('robots-parser');

class Axios {
    waitingForSlot = [];
    visitedLinks = new Set();
    robotsTxTCache = new Map();

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
                if(err.response.status === 404) {
                    return { data: "User-agent: *\nAllow: *\n" };
                }
            })).data;
            this.robotsTxTCache.set(robotsTxTUrl, robotsTxTContent);
        }
        
        if(robotsParser(robotsTxTUrl, robotsTxTContent).isDisallowed(url) ?? false) {
            return null;
        }

        await this._waitForSlot();
        console.log(url);
        const res = await axios.get(url).catch(err => console.log(`Errored with code: ${err.response.status} at url ${url}`)).catch(console.error);
        if (this.waitingForSlot.length > 0) {
            this.waitingForSlot.shift().resolve();
        } else {
            this.availableSlots++;
        }

        if(!res) {
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
                const parsedURL = new URL(link, url);
                if(parsedURL.protocol !== 'https:' && parsedURL.protocol !== 'http:') {
                    return null;
                }
                return parsedURL.href;
            }).filter(x => x !== null)
        };
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