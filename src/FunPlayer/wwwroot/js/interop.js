
const audio = new Audio();

audio.addEventListener('canplaythrough',
    function() {
        DotNet.invokeMethodAsync("FunPlayer", "updatePlayStatus", "is-ready");
    });
audio.addEventListener('play', event => {
    // dispose urls when they are played
    if (event.target instanceof HTMLAudioElement) {
        URL.revokeObjectURL(event.target.src);
    }
    DotNet.invokeMethodAsync("FunPlayer", "updatePlayStatus", "is-playing");
});
audio.addEventListener('ended',
    function() {
        DotNet.invokeMethodAsync("FunPlayer", "updatePlayStatus", "it-stoped");
    });

audio.addEventListener('timeupdate',
    function(event) {
        const target = /** @type {HTMLAudioElement} */ (/** @type {unknown} */ event.target);
        DotNet.invokeMethodAsync("FunPlayer", "timeUpdate", target.currentTime);
    });
audio.addEventListener('durationchange',
    function(event) {
        const target = /** @type {HTMLAudioElement} */ (/** @type {unknown} */ event.target);
        DotNet.invokeMethodAsync("FunPlayer", "durationChanged", target.duration);
    });


/**
 * @type {File[]}
 */
const files = [];


/**
 * 
 * @param {string} name
 */
async function play(name) {
    const file = files.find(f => f.name === name);
    audio.src = URL.createObjectURL(file);
    return audio.play();
}

/**
 * 
 * @returns {Promise<void>}
 */
async function loadFiles() {
    const handle = await window.showDirectoryPicker();
    files.splice(0, files.length);
    for await (const [, fileHandle] of handle) {
        if (fileHandle instanceof FileSystemFileHandle) {
            /**
             * @type {File}
             */
            const file = await fileHandle.getFile();
            if (file.type.includes("audio/")) {
                files.push(file);
            }
        }
    }
}

window.FunPlayer = {
    play,
    loadFiles,
    getFiles: () => files.map(f => f.name),
    pause: () => audio.pause(),
    unpause: () => audio.play(),
    supportsWindowControlsOverlay: () => 'windowControlsOverlay' in navigator && navigator['windowControlsOverlay'].visible,
    supportsFileSystemAccess: () => 'showOpenFilePicker' in window
};
