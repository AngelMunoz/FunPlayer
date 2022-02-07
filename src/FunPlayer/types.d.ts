
type Player = {
    play(name: string): Promise<void>;
    pause(): void;
};

type FileManager = {
    getFiles(): string[];
    loadFiles(): Promise<void>;
};

type BrowserSupport = {
    supportsWindowControlsOverlay(): boolean;
    supportsFileSystemAccess(): boolean;
};

declare const DotNet: {
    invokeMethodAsync(assembly: string, methodName: string, ...args: any[]): Promise<any>,
    invokeMethod(assembly: string, methodName: string, ...args: any[]): any,
};

declare interface Window {
    FunPlayer: FileManager & Player & BrowserSupport;
    DotNet: any;
}