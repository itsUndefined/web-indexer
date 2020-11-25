const axios = require('axios');
const { JSDOM } = require('jsdom');

class Axios {
    waitingForSlot = [];
    visitedLinks = new Set();

    constructor(parallelRequests) {
        this.parallelRequests = parallelRequests;
        this.availableSlots = this.parallelRequests;
    }

    async request(url) {
        if(this.visitedLinks.has(url)) {
            return null;
        }
        this.visitedLinks.add(url);
        await this._waitForSlot();
        console.log(url);
        const res = await axios.get(url).catch(err => console.log(`Errored with code: ${err.response.status} at url ${url}`));
        if (this.waitingForSlot.length > 0) {
            this.waitingForSlot.shift().resolve();
        } else {
            this.availableSlots++;
        }

        if(!res) {
            return null;
        }
        
        const dom = new JSDOM(res.data).window.document;
        let links = [];
        dom.querySelectorAll('a').forEach(link => {
            links.push(link.href);
        });
        let pageInfo = {
            title: dom.querySelector('title')?.textContent.trim(),
            url: res.config.url,
            text: (dom.querySelector('body')?.textContent ?? '')
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