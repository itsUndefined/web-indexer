const jsdom = require('jsdom');
const axios = require('axios');

const { JSDOM } = jsdom;

axios.get('https://example.org/')
    .then(res => {
        const dom = new JSDOM(res.data).window;
        console.log('TEXT:');
        console.log(dom.document.querySelector('body').textContent.trim());
        console.log('LINKS:');
        dom.document.querySelectorAll('a').forEach(link => {
            console.log(link.href);
        });
    });

