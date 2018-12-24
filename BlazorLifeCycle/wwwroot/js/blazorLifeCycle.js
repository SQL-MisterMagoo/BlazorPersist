window.blazorLifeCycle = {
    savecookie: function (name, value) {
        document.cookie = name + "=" + value;
        return true;
    },
    readcookie: function () {
        return document.cookie;
    }
};
window.onbeforeunload = function () {
    return DotNet.invokeMethodAsync("BlazorLifeCycle", "onbeforeunload");
};