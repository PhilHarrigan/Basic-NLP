function startGraph() {
    document.getElementById("containerDiv").classList.remove("hidden");
    console.log("This is what I loaded: " + document.getElementById("data").getAttribute("dataJSON"));
    console.log("From this article : " + document.getElementById("currentArticle").getAttribute("currentArticle"));
    (function (d3$1) {
        'use strict';
        const nodes = [];
        const links = [];
        var data = {};
        var loadJSON = JSON.parse(document.getElementById("data").getAttribute("dataJSON"));
        data = loadJSON;
        var nodeIndex = 0;
        var nodeDic = {};
        
        window.onresize = updateSVG;

        for (var key in data) {
            nodes.push({ id: key.trim(), connections: data[key] });
            nodeDic[key] = nodeIndex;
            nodeIndex++;
        }
        nodeIndex = 0;
        for (var key in data) {
            if (data[key].length < 1) {
                nodeIndex++;
                continue;
            }

            for (var link in data[key]) {
                links.push({ source: nodeDic[key], target: nodeDic[data[key][link].replace(/[^a-zA-Z0-9 ]+/g, "")] });
            }
            nodeIndex++;
        }
        const svg = d3.select('#container');
        var centerX = +document.getElementById("containerDiv").offsetWidth / 2;
        var centerY = +document.getElementById("containerDiv").offsetHeight / 2;
        var forceStrength = -centerX / 15;
        if (forceStrength < - 35) { forceStrength == -35 }
        const simulation = d3
            .forceSimulation(nodes)
            .force('charge', d3$1.forceManyBody().strength(-35))
            .force('link', d3$1.forceLink())
            .force('center', d3$1.forceCenter(centerX, centerY));

        const lines = svg
            .selectAll('line')
            .data(links)
            .enter()
            .append('line')
            .attr('stroke', '#002FC7')
            .attr("stroke-width", 2);

        const circles = svg
            .selectAll('circle')
            .data(nodes)
            .enter()
            .append('circle')
            .attr('r', 40)
            .attr('z', 5)
            .on("click", click)
            .on("mouseover", hover)
            .on("mouseout", leave)
            .attr('fill', '#002395');

        const text = svg
            .selectAll('text')
            .data(nodes)
            .enter()
            .append('text')
            .attr('text-anchor', 'middle')
            .attr('alignment-baseline', 'middle')
            .style("font-family", "Montserrat")
            .style('pointer-events', 'none')
            .attr("fill", "white")
            .text(function (node) {
                var text = node.id;
                return text.substring(0, 8) + "...";
            })

        simulation.on('tick', () => {
            circles
                .attr('cx', (node) => node.x)
                .attr('cy', (node) => node.y)
                .attr("relations", (node) => node.connections)
                .attr("name", (node) => node.id);
            text
                .attr('x', (node) => node.x)
                .attr('y', (node) => node.y);
            lines
                .attr("name", (link) => [nodes[link.source].id, nodes[link.target].id])
                .attr('x1', (link) => nodes[link.source].x)
                .attr('y1', (link) => nodes[link.source].y)
                .attr('x2', (link) => nodes[link.target].x)
                .attr('y2', (link) => nodes[link.target].y);
        });
        function click() {
            //d3.select(this).transition().duration(750).attr('r', 50);
            var wrapper = document.getElementById("containerDiv");
            var cover = document.createElement("div");
            cover.classList.add("cover");
            cover.addEventListener("click", function (e) {
                if (e.target == e.currentTarget) {
                    cover.remove();
                }
            })
            var nodeName = this.getAttribute("name");
            var nodeInfo = document.createElement("div");
            nodeInfo.classList.add("nodeInfo");
            // nodeInfo.innerHTML = nodeName;
            var titleDisplay = document.createElement("div");
            titleDisplay.id = "titleDisplay";
            titleDisplay.innerHTML = nodeName;
            var dataDisplay = document.createElement('div');
            dataDisplay.id = "dataDisplay";
            var dataJSON = JSON.parse(DotNet.invokeMethod('NLP_API.Client', 'callGetSubjectJSON', nodeName));
            var headers = document.createElement("div");
            headers.classList.add("dataWrap");
            headers.innerHTML = `<div class="dataItem dataItemTitle">Related Subjects</div><div class="dataItem dataItemTitle">Primary Verb</div>
                                 <div class="dataItem dataItemTitle">Secondary Verbs</div><div class="dataItem dataItemTitle">Article #</div><div class="dataItem dataItemTitle">Sentence #</div>`;
            dataDisplay.appendChild(headers);
            for (key in dataJSON) {
                var dataWrap = document.createElement('div');
                dataWrap.classList.add("dataWrap");
                var otherSubs = document.createElement('div');
                otherSubs.classList.add("dataItem");
                otherSubs.innerHTML = dataJSON[key]["relatedSubjects"].join(", ").replace(/\w\S*/g, (w) => (w.replace(/^\w/, (c) => c.toUpperCase())));

                var primeVerb = document.createElement('div');
                primeVerb.classList.add("dataItem");
                primeVerb.innerHTML = key;

                var secVerbs = document.createElement('div');
                secVerbs.classList.add("dataItem");
                secVerbs.innerHTML = dataJSON[key]["secondaryVerbs"].join(", ");

                var article = document.createElement("div");
                article.classList.add("dataItem");
                article.innerHTML = dataJSON[key]["article"];

                var sentence = document.createElement("div");
                sentence.classList.add("dataItem");
                sentence.innerHTML = dataJSON[key]['line'];

                dataWrap.appendChild(otherSubs);
                dataWrap.appendChild(primeVerb);
                dataWrap.appendChild(secVerbs);
                dataWrap.appendChild(article);
                dataWrap.appendChild(sentence);
                dataDisplay.appendChild(dataWrap);
            }
            //dataDisplay.innerHTML = DotNet.invokeMethod('NLP_API.Client', 'callGetSubjectJSON', nodeName);
            nodeInfo.appendChild(titleDisplay);
            nodeInfo.appendChild(dataDisplay);
            cover.appendChild(nodeInfo);
            cover.style.zIndex = "50";
            wrapper.appendChild(cover);
        }
        function hover() {
            d3.select(this).transition().duration(100).attr("fill", "#003BFA");
            var name = this.getAttribute("name");
            var otherNodesUnsplit = this.getAttribute("relations");
            var otherNodes = otherNodesUnsplit.split(",");
            svg.selectAll("line").filter(function () { return d3.select(this).attr("name").split(",").includes(name) }).transition().duration(25).attr("stroke", "#0038F0");
            for (var node in otherNodes) {
                svg.selectAll("circle").filter(function () { return d3.select(this).attr("name") == otherNodes[node] }).transition().duration(100).attr("fill", "#003BFA");
                var connectedNodes = svg.selectAll("circle").filter(function () { return d3.select(this).attr("name") == otherNodes[node] })._groups;
                if (connectedNodes[0].length > 0) {
                    displayTooltip(connectedNodes[0][0]);
                }
            }
            displayTooltip(this);
        }
        function leave() {
            d3.select(this).transition().duration(100).attr("fill", "#002395");
            var otherNodesUnsplit = this.getAttribute("relations");
            var otherNodes = otherNodesUnsplit.split(",");
            svg.selectAll("line").filter(function () { return d3.select(this).attr("name").includes(name) }).transition().duration(25).attr("stroke", "#002FC7");
            for (var node in otherNodes) {
                svg.selectAll("circle").filter(function () { return d3.select(this).attr("name") == otherNodes[node] }).transition().duration(100).attr("fill", "#002395");
            }
            svg.selectAll("rect").remove();
            svg.selectAll("text").filter(function () { return d3.select(this).attr("removable") == "true" }).remove();
        }
        function updateSVG() {
            centerX = +document.getElementById("containerDiv").offsetWidth / 2;
            centerY = +document.getElementById("containerDiv").offsetHeight / 2;
            svg.selectAll("*").remove();
            startGraph();
        }
        function displayTooltip(node) {
            var svgns = "http://www.w3.org/2000/svg";
            var rect = document.createElementNS(svgns, 'rect');
            rect.setAttribute('x', + node.getAttribute("cx") - 5);
            rect.setAttribute('y', node.getAttribute("cy") - 18);
            rect.setAttribute('height', '25');
            rect.setAttribute('fill', '#003BFA');
            rect.setAttribute('stroke', "white");
            rect.setAttribute("pointer-events", "none");
            rect.setAttribute("position", "absolute");
            rect.setAttribute("text", node.getAttribute("id"));
            var text = document.createElementNS(svgns, "text");
            text.textContent = node.getAttribute("name");
            text.setAttribute('x', + node.getAttribute("cx"));
            text.setAttribute('y', node.getAttribute("cy"));
            text.setAttribute("removable", "true");
            text.setAttribute("width", 100);
            text.setAttribute("height", 25);
            text.setAttribute("position", "absolute");
            text.setAttribute("pointer-events", "none");
            document.getElementById('container').appendChild(text);
            var len = text.getComputedTextLength();
            rect.setAttribute('width', len + 10);
            document.getElementById('container').appendChild(rect);
            document.getElementById('container').appendChild(text);
        }
        // function hoverTest(){
        //   d3.select(this).transition().duration(100).attr("stroke", "yellow");
        //   console.log(this);
        // }
    }(d3));

}


  //# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJmaWxlIjoiaW5kZXguanMiLCJzb3VyY2VzIjpbImRhdGEuanMiLCJpbmRleC5qcyJdLCJzb3VyY2VzQ29udGVudCI6WyJleHBvcnQgY29uc3Qgbm9kZXMgPSBbXTtcbmV4cG9ydCBjb25zdCBsaW5rcyA9IFtdO1xuY29uc3QgYWRkTm9kZSA9IChub2RlKSA9PiB7XG4gIG5vZGVzLnB1c2gobm9kZSk7XG59O1xuXG5jb25zdCBjb25uZWN0Tm9kZXMgPSAoc291cmNlLCB0YXJnZXQpID0+IHtcbiAgbGlua3MucHVzaCh7XG4gICAgc291cmNlLFxuICAgIHRhcmdldCxcbiAgfSk7XG59O1xuXG52YXIgdGVzdFNvbiA9IHtcbiAgb25lOiB7XG4gICAgbmFtZTogJ1hpJyxcbiAgICBjb25uZWN0aW9uczogWydCaWRlbiddLFxuICB9LFxuICB0d286IHtcbiAgICBuYW1lOiAnQmlkZW4nLFxuICAgIGNvbm5lY3Rpb25zOiBbJ1hpJywgJ1RoZSBXaGl0ZSBIb3VzZSddLFxuICB9LFxuICB0aHJlZToge1xuICAgIG5hbWU6ICdUaGUgV2hpdGUgSG91c2UnLFxuICAgIGNvbm5lY3Rpb25zOiBbJ0JpZGVuJ10sXG4gIH0sXG59O1xuXG5mb3IgKHZhciBrZXkgaW4gdGVzdFNvbikge1xuICBjb25zb2xlLmxvZyh0ZXN0U29uW2tleV1bJ25hbWUnXSk7XG4gIG5vZGVzLnB1c2godGVzdFNvbltrZXldWyduYW1lJ10pO1xuICBsaW5rcy5wdXNoKHtcbiAgICBzb3VyY2U6e2lkOnRlc3RTb25ba2V5XVsnbmFtZSddfSxcbiAgICB0YXJnZXQ6e2lkOnRlc3RTb25ba2V5XVsnY29ubmVjdGlvbnMnXVswXX1cbn0pO1xufVxuIiwiaW1wb3J0IHtcbiAgZm9yY2VTaW11bGF0aW9uLFxuICBmb3JjZU1hbnlCb2R5LFxuICBmb3JjZUxpbmssXG4gIGZvcmNlQ2VudGVyLFxuICBzZWxlY3QsXG59IGZyb20gJ2QzJztcbmltcG9ydCB7IG5vZGVzLCBsaW5rcyB9IGZyb20gJy4vZGF0YS5qcyc7XG5cbi8vIGNvbnN0IG5vZGVzID0gW1xuLy8gICB7IGlkOiAnUGhpbCcgfSxcbi8vICAgeyBpZDogJ01pa2UnIH0sXG4vLyAgIHsgaWQ6ICdKb25ueScgfSxcbi8vIF07XG5cbi8vIGNvbnN0IGxpbmtzID0gW1xuLy8gICB7IHNvdXJjZTogMCwgdGFyZ2V0OiAxIH0sXG4vLyAgIHsgc291cmNlOiAxLCB0YXJnZXQ6IDIgfSxcbi8vICAgeyBzb3VyY2U6IDAsIHRhcmdldDogMiB9LFxuLy8gXTtcbmNvbnN0IHN2ZyA9IGQzLnNlbGVjdCgnI2NvbnRhaW5lcicpO1xuY29uc3QgY2VudGVyWCA9ICtzdmcuYXR0cignd2lkdGgnKSAvIDI7XG5jb25zdCBjZW50ZXJZID0gK3N2Zy5hdHRyKCdoZWlnaHQnKSAvIDI7XG5jb25zdCBzaW11bGF0aW9uID0gZDNcbiAgLmZvcmNlU2ltdWxhdGlvbihub2RlcylcbiAgLmZvcmNlKCdjaGFyZ2UnLCBmb3JjZU1hbnlCb2R5KCkuc3RyZW5ndGgoLTMwKSlcbiAgLmZvcmNlKCdsaW5rJywgZm9yY2VMaW5rKCkpXG4gIC5mb3JjZSgnY2VudGVyJywgZm9yY2VDZW50ZXIoY2VudGVyWCwgY2VudGVyWSkpO1xuXG5jb25zdCBsaW5lcyA9IHN2Z1xuICAuc2VsZWN0QWxsKCdsaW5lJylcbiAgLmRhdGEobGlua3MpXG4gIC5lbnRlcigpXG4gIC5hcHBlbmQoJ2xpbmUnKVxuICAuYXR0cignc3Ryb2tlJywgJ2JsYWNrJyk7XG5cbmNvbnN0IGNpcmNsZXMgPSBzdmdcbiAgLnNlbGVjdEFsbCgnY2lyY2xlJylcbiAgLmRhdGEobm9kZXMpXG4gIC5lbnRlcigpXG4gIC5hcHBlbmQoJ2NpcmNsZScpXG4gIC5hdHRyKCdyJywgMjUpXG4gIC5hdHRyKCd6JywgNSlcbiAgLmF0dHIoJ2ZpbGwnLCAnZ3JlZW4nKTtcblxuY29uc3QgdGV4dCA9IHN2Z1xuICAuc2VsZWN0QWxsKCd0ZXh0JylcbiAgLmRhdGEobm9kZXMpXG4gIC5lbnRlcigpXG4gIC5hcHBlbmQoJ3RleHQnKVxuICAuYXR0cigndGV4dC1hbmNob3InLCAnbWlkZGxlJylcbiAgLmF0dHIoJ2FsaWdubWVudC1iYXNlbGluZScsICdtaWRkbGUnKVxuICAudGV4dCgobm9kZSkgPT4gbm9kZS5pZCk7XG5cbnNpbXVsYXRpb24ub24oJ3RpY2snLCAoKSA9PiB7XG4gIGNpcmNsZXNcbiAgICAuYXR0cignY3gnLCAobm9kZSkgPT4gbm9kZS54KVxuICAgIC5hdHRyKCdjeScsIChub2RlKSA9PiBub2RlLnkpO1xuICB0ZXh0XG4gICAgLmF0dHIoJ3gnLCAobm9kZSkgPT4gbm9kZS54KVxuICAgIC5hdHRyKCd5JywgKG5vZGUpID0+IG5vZGUueSk7XG4gIGxpbmVzXG4gICAgLmF0dHIoJ3gxJywgKGxpbmspID0+IG5vZGVzW2xpbmsuc291cmNlXS54KVxuICAgIC5hdHRyKCd5MScsIChsaW5rKSA9PiBub2Rlc1tsaW5rLnNvdXJjZV0ueSlcbiAgICAuYXR0cigneDInLCAobGluaykgPT4gbm9kZXNbbGluay50YXJnZXRdLngpXG4gICAgLmF0dHIoJ3kyJywgKGxpbmspID0+IG5vZGVzW2xpbmsudGFyZ2V0XS55KTtcbn0pO1xuIl0sIm5hbWVzIjpbImZvcmNlTWFueUJvZHkiLCJmb3JjZUxpbmsiLCJmb3JjZUNlbnRlciJdLCJtYXBwaW5ncyI6Ijs7O0VBQU8sTUFBTSxLQUFLLEdBQUcsRUFBRSxDQUFDO0VBQ2pCLE1BQU0sS0FBSyxHQUFHLEVBQUUsQ0FBQztBQVd4QjtFQUNBLElBQUksT0FBTyxHQUFHO0VBQ2QsRUFBRSxHQUFHLEVBQUU7RUFDUCxJQUFJLElBQUksRUFBRSxJQUFJO0VBQ2QsSUFBSSxXQUFXLEVBQUUsQ0FBQyxPQUFPLENBQUM7RUFDMUIsR0FBRztFQUNILEVBQUUsR0FBRyxFQUFFO0VBQ1AsSUFBSSxJQUFJLEVBQUUsT0FBTztFQUNqQixJQUFJLFdBQVcsRUFBRSxDQUFDLElBQUksRUFBRSxpQkFBaUIsQ0FBQztFQUMxQyxHQUFHO0VBQ0gsRUFBRSxLQUFLLEVBQUU7RUFDVCxJQUFJLElBQUksRUFBRSxpQkFBaUI7RUFDM0Is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QUFJLENBQUMsSUFBSSxFQUFFLENBQUMsSUFBSSxLQUFLLElBQUksQ0FBQyxDQUFDLENBQUM7RUFDakMsS0FBSyxJQUFJLENBQUMsSUFBSSxFQUFFLENBQUMsSUFBSSxLQUFLLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQztFQUNsQyxFQUFFLElBQUk7RUFDTixLQUFLLElBQUksQ0FBQyxHQUFHLEVBQUUsQ0FBQyxJQUFJLEtBQUssSUFBSSxDQUFDLENBQUMsQ0FBQztFQUNoQyxLQUFLLElBQUksQ0FBQyxHQUFHLEVBQUUsQ0FBQyxJQUFJLEtBQUssSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDO0VBQ2pDLEVBQUUsS0FBSztFQUNQLEtBQUssSUFBSSxDQUFDLElBQUksRUFBRSxDQUFDLElBQUksS0FBSyxLQUFLLENBQUMsSUFBSSxDQUFDLE1BQU0sQ0FBQyxDQUFDLENBQUMsQ0FBQztFQUMvQyxLQUFLLElBQUksQ0FBQyxJQUFJLEVBQUUsQ0FBQyxJQUFJLEtBQUssS0FBSyxDQUFDLElBQUksQ0FBQyxNQUFNLENBQUMsQ0FBQyxDQUFDLENBQUM7RUFDL0MsS0FBSyxJQUFJLENBQUMsSUFBSSxFQUFFLENBQUMsSUFBSSxLQUFLLEtBQUssQ0FBQyxJQUFJLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDO0VBQy9DLEtBQUssSUFBSSxDQUFDLElBQUksRUFBRSxDQUFDLElBQUksS0FBSyxLQUFLLENBQUMsSUFBSSxDQUFDLE1BQU0sQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDO0VBQ2hELENBQUMsQ0FBQzs7OzsifQ==