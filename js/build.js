const fs = require('fs');
const handlebars = require("handlebars");

const src = fs.readFileSync('index.js');

fs.mkdirSync('dist');
['browser', 'node', 'module'].forEach((e) => {
	fs.writeFileSync('dist/'+e+'.js', handlebars.compile(fs.readFileSync('templates/'+e+'.js.hbs').toString('utf8'))({src}));
});
