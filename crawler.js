const jsdom = require('jsdom');
const axios = require('axios');

const { JSDOM } = jsdom;

axios.get('https://example.org/')
    .then(res => {
        const dom = new JSDOM(res.data).window.document;
        let links = [];
        dom.querySelectorAll('a').forEach(link => {
            links.push(link.href);
        });
        let pageInfo = {
            title: dom.querySelector('title').textContent.trim(),
            url: res.config.url,
            text: dom.querySelector('body').textContent
                .trim()
                .toLowerCase()
                .replace(/[\n,.?!@#$%^&*()-=/*+<>|_`~]/g, '')
                .replace(/\s+/g, ' '),
            links: links
        };
        console.log(pageInfo);
    });

