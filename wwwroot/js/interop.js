
const audio = new Audio();
/**
 * @type {File[]}
 */
const files = [];

// dispose urls when they are played
audio.addEventListener('play', event => {
    if (event.target instanceof HTMLAudioElement) {
        URL.revokeObjectURL(event.target.src);
    }
});

/**
 * 
 * @param {string} name
 */
async function play(name) {
    const file = files.find(f => f.name === name);
    const url = URL.createObjectURL(file);
    audio.src = url;
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
            const file = await fileHandle.getFile();
            if(file.type.includes("audio/")) {
                files.push(file);
            }
        }
    }
}

/**
 * 
 * @type {Player & FileManager}
 */
window.FunPlayer = {
    play,
    loadFiles,
    getFiles: () => files.map(f => f.name),
    pause: () => audio.pause(),
};
