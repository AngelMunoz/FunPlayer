
type Player = {
    play(name: string): Promise<void>;
    pause(): void;
};

type FileManager = {
    getFiles(): string[];
    loadFiles(): Promise<void>;
};

declare interface Window {
    FunPlayer: FileManager & Player;
}