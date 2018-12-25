const EXP_NAME = '_expires';

window.blazorLifeCycle = {

    savedata: function (name, value) {
        if (name && value) {
            console.log("JS:SAVE:" + name);
            localStorage.setItem(name, value);
            return false;
        }
        return true;
    },
    readdata: function (name) {
        console.log("JS:READ: " + name);
        return localStorage.getItem(name);
    },
    cleardata: function (name) {
        console.log("JS:CLEAR: " + name);
        return localStorage.removeItem(name);
    }
};
window.onbeforeunload = function () {
    DotNet.invokeMethodAsync("BlazorLifeCycle", "onbeforeunload");
};