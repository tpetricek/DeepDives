
F# Deep Dives: Numerical computing in the financial domain
====================================================
Chao-Jen Chen, 2014

This repository contains accompanying material for Chapter 4 
of [F# Deep Dives](http://www.manning.com/petricek2/).

Content
---------------

- `ExtractNetwork.fsx` downloads network around [F# Software Foundation](https://twitter.com/fsharporg)
 from Twitter and saves it into JSON files.

- `AnalyzeNetwork.fsx` loads saved JSON data into F# and performs exploratory analysis
to identify important nodes in the social network.

- `data\fsharporgLinks.json`, `data\fsharporgNodes.json` are files with data downloaded from Twitter.

- `data\pageRankNodes.json` adds PageRank value of each user to `fsharporgNodes.json`

- `data\d3_twitter.html` visualizes Twitter network around [F# Software Foundation](https://twitter.com/fsharporg)
on Twitter using D3.js

- `data\d3_twitterPageRank.html` visualizes Twitter network aroun [F# Software Foundation](https://twitter.com/fsharporg)
and scales nodes (users) proportionally to their PageRank centrality.

