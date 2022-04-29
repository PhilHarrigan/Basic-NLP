//displays the data input for functionality
function displayTextArea() {
    document.getElementById("dataInputWrapper").classList.remove("hidden");
}
//takes the information in the dataInputWrapper and sends it to the database to be parsed.
function sendArticle() {
    var articleName = document.getElementById("articleName");
    var articleContent = document.getElementById("dataInput");

    if (articleName.value == "" || articleContent.value == "") {
        if (articleName.value == "") {
            articleName.classList.add("error");
        }
        if (articleContent.value == "") {
            articleContent.classList.add("error");
        }
        alert("Article Name and Article Value must both have content.")
    } else {
        if (articleName.value.length > 100) {
            articleName.classList.add("error");
            alert("Article name can't be more than 100 characters.");
        }
        else if (articleContent.value.split(" ").length < 50 || articleContent.value.indexOf(".") == -1) {
            articleContent.classList.add("error");
            alert("Article must be at least 50 words long and contain one period");
        }
        else {
            var articleList = document.getElementById("articlesList");
            var articleNum = JSON.parse(articleList.getAttribute("articleList"));
            //console.log(articleNum[articleNum.length - 1]["Item1"]);
            //try {
            //    document.getElementById("currentArticle").setAttribute("currentArticle", articleNum[articleNum.length - 1]["Item1"] + 1);
            //    document.getElementById("currentArticle").innerHTML = document.getElementById("currentArticle").getAttribute("currentArticle");
            //} catch (e) {
            //    document.getElementById("currentArticle").setAttribute("currentArticle", 1);
            //}
            if (articleNum.length != 0) {
                document.getElementById("currentArticle").setAttribute("currentArticle", articleNum[articleNum.length - 1]["Item1"] + 1);
            } else {
                document.getElementById("currentArticle").setAttribute("currentArticle", document.getElementById("currentArticle").getAttribute("currentArticle") + 1);
            }
            document.getElementById("dataInputWrapper").classList.add("hidden");
            console.log(articleContent.value);
            return articleName.value + "|||" + articleContent.value;
        }
    }
}
//function for the "Get old article" button displayes article selector and removes old article button
function displayArticleSelection() {
    document.getElementById("articleSelect").classList.remove("hidden");
    document.getElementById("oldArticle").classList.add("hidden");
}
function buildDropDown() {
    var host = document.getElementById("articleSelect");
    var data = JSON.parse(document.getElementById("articlesList").getAttribute("articleList"));
    var wrapper = document.createElement("div");
    wrapper.classList.add("ddWrapper");
    for (var item in data) {
        var option = document.createElement("div");
        option.classList.add("ddOption");
        option.innerHTML = data[item]["Item2"];
        option.value = data[item];
        option.addEventListener("click", function () {
            host.innerHTML = this.innerHTML;
            document.getElementById("currentArticle").setAttribute("currentArticle", this.value["Item1"]);
            document.getElementById("currentArticle").innerHTML = this.value["Item1"];
            document.getElementById("startGraph").removeAttribute("disabled");
            DotNet.invokeMethod('NLP_API.Client', 'ddLeave');
        });
        wrapper.appendChild(option);
    }
    host.appendChild(wrapper);
    host.addEventListener("mouseleave", function () {
        wrapper.remove();
        DotNet.invokeMethod('NLP_API.Client', 'ddLeave');
    })
}
//retrieves the article in the DOM. Called from cStartGraph. Also hides the Start Graph button and unhides the Clear Graph button
function getCurrentArticle() {
    document.getElementById("startGraph").classList.add("hidden");
    document.getElementById("clearGraph").classList.remove("hidden");
    document.getElementById("oldArticle").classList.add("hidden");
    document.getElementById("articleSelect").classList.add("hidden");
    document.getElementById("article").classList.add("hidden");
    return parseInt(document.getElementById("currentArticle").getAttribute("currentArticle"));
}
// new article get current article
function newArticleGetCurrentArticle() {
    document.getElementById("startGraph").removeAttribute("disabled");
    document.getElementById("oldArticle").classList.add("hidden");
    document.getElementById("article").classList.add("hidden");
    return parseInt(document.getElementById("currentArticle").getAttribute("currentArticle"));
}
function clearGraph() {
    var svg = document.getElementById("container");
    svg.remove();
    var newSVG = document.createElement("svg");
    newSVG.id = "container";
    document.getElementById("containerDiv").appendChild(newSVG);
    document.getElementById("startGraph").classList.remove("hidden");
    document.getElementById("clearGraph").classList.add("hidden");
    location.reload();
}
function sendAlert() {
    alert("Something went wrong :(");
}
function loading() {
    var wrapper = document.getElementById("wrapper");
    var cover = document.createElement("div");
    cover.classList.add("cover");
    var loader = document.createElement('div');
    loader.id = "loader";
    loader.innerHTML = "LOADING...";
    cover.appendChild(loader);
    wrapper.appendChild(cover);
}
function removeLoading() {
    var cover = document.querySelector(".cover");
    cover.remove();
}
function testing() {
    console.log(document.getElementById("data").getAttribute("dataJSON"));
}
//document.getElementById("articleName").addEventListener("focus", (e) => {
//    e.target.classList.remove("error");
//})
//document.getElementById("dataInput").addEventListener("focus", (e) => {
//    e.target.classList.remove("error");
//})