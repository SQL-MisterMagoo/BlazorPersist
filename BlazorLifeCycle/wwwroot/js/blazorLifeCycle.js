const EXP_NAME = '_expires';

window.blazorLifeCycle = {

    savedata: function (name, value, timeout) {
        if (name && value) {
            localStorage.setItem(name, value);
            console.log("JS:SAVE:" + name + "," + value + "," + timeout);
            if (timeout > 0) {
                const expireDate = new Date(Date.now() + timeout * 60000);
                console.log("JS:SAVE: Expires : " + expireDate.toLocaleDateString());
                const data = JSON.stringify({ "expires": expireDate.toISOString(), "timeout": timeout });
                console.log("JS:SAVE: Expires data :" + data);
                localStorage.setItem(name + EXP_NAME, data );
            }
        }
        return true;
    },
    readdata: function (name) {
        const expires = localStorage.getItem(name + EXP_NAME);
        console.log("JS:READ:" + name + " expires=" + expires);
        if (expires) {
            const data = JSON.parse(expires);
            const dateExpires = new Date( Date.parse( data.expires ) );
            console.log("JS:READ:" + name + " dateExpires=" + dateExpires.toTimeString());
            if (dateExpires) {
                if (dateExpires < Date.now()) {
                    console.log("JS:READ:Removing expired " + name);
                    localStorage.removeItem(name);
                    localStorage.removeItem(name + EXP_NAME);
                }
            }
        }
        console.log("JS:READ:Reading " + name);
        return localStorage.getItem(name);
    },
    listdata: function () {
        return Object.keys(localStorage);
    }
};
window.onbeforeunload = function () {
    DotNet.invokeMethodAsync("BlazorLifeCycle", "onbeforeunload");
};