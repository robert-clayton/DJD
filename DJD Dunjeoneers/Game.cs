using Godot;
using System;
using System.Collections.Generic;

using EnemyGenerator = System.Func<Godot.Vector2, Godot.Vector2, Godot.Vector2, Enemy>;

public class Game : Node2D{
    private Floor floor;
    private Player player;
    private readonly List<List<(EnemyGenerator, int)>> waveDefinitions = new List<List<(EnemyGenerator, int)>>();
    private int currentWave = 0;
    private AudioStreamPlayer bgm;
    private Globals globals;

    
    
    public Game(){
        Enemy Slime(Vector2 p, Vector2 s, Vector2 e) => new SlimeBasic(p, s, e);
        Enemy FireSlime(Vector2 p, Vector2 s, Vector2 e) => new SlimeFire(p, s, e);
        Enemy Golem(Vector2 p, Vector2 s, Vector2 e) => new Golem(p, s, e);

        var waveOne = new List<(EnemyGenerator, int)> {(Slime, 5)};

        var waveTwo = new List<(EnemyGenerator, int)> {(Slime, 15), (FireSlime, 3)};

        var waveThree = new List<(EnemyGenerator, int)> {(Slime, 20), (FireSlime, 5)};

        var waveFour = new List<(EnemyGenerator, int)> {(Slime, 30), (FireSlime, 15)};

        var waveFive = new List<(EnemyGenerator, int)> {(Slime, 30), (FireSlime, 30)};

        var waveSix = new List<(EnemyGenerator, int)> {(Golem, 1)};

        waveDefinitions.Add(waveOne);
        waveDefinitions.Add(waveTwo);
        waveDefinitions.Add(waveThree);
        waveDefinitions.Add(waveFour);
        waveDefinitions.Add(waveFive);
        waveDefinitions.Add(waveSix);
    }

    public override void _Ready(){
        globals = GetTree().Root.GetNode("Globals") as Globals;
        floor = GetNode<Floor>("Floor");
        bgm = GetNode<AudioStreamPlayer>("BGM");

        globals.Connect("MusicDbChanged", this, "_OnMusicDbChanged");
        globals.Connect("MusicPaused", this, "_OnMusicPaused");
    }

    public void _OnMusicDbChanged(float value){
        bgm.VolumeDb = value;
    }

    public void _OnMusicPaused(bool enabled){
        bgm.StreamPaused = enabled;
    }

    public void Reset(){
        foreach (Enemy enemy in GetTree().GetNodesInGroup("Enemies"))
            enemy.QueueFree();
        currentWave = 0;
        if (IsInstanceValid(player)) player.QueueFree();
        player = new Player((floor.areaStart + floor.areaEnd)/2);
        player.Connect("Dead", this, "Reset");
        AddChild(player);
        SpawnEnemies(waveDefinitions[currentWave]);
    }

    public override void _Process(float delta){
        if (GetTree().GetNodesInGroup("Enemies").Count == 0){
            if (++currentWave < waveDefinitions.Count){
                SpawnEnemies(waveDefinitions[currentWave]);
            }
        }
    }

    private void SpawnEnemies(List<(EnemyGenerator, int)> waveDefinition){
        foreach (var (constructEnemy, count) in waveDefinition){
            for (int i = 0; i < count; i++){
                Vector2 newSpawnPosition = floor.GetRandomSpawnLocation();
                var enemy = constructEnemy(newSpawnPosition, floor.areaStart, floor.areaEnd);
                GetTree().Root.CallDeferred("add_child", enemy);
            }
        }
    }

    public void SaveGame(string fileName = "savegame"){
        File saveGame = new File();
        saveGame.Open($"user://{fileName}.dfd", File.ModeFlags.Write);

        foreach (Node saveNode in GetTree().GetNodesInGroup("Persist")){
            Dictionary<string, object> data = saveNode.Call("GetState") as Dictionary<string, object>;
            saveGame.StoreLine(JSON.Print(data));
        }
        saveGame.Close();
    }

    public void LoadGame(string fileName){
        File saveGame = new File();
        if (!saveGame.FileExists($"user://{fileName}.dfd"))
            return;
        
        // Remove Persist group nodes so they won't be doubled after load
        foreach (Node saveNode in GetTree().GetNodesInGroup("Persist"))
            saveNode.QueueFree();
        
        saveGame.Open($"user://{fileName}.dfd", File.ModeFlags.Read);
        
        while (!saveGame.EofReached()){
            Dictionary<object, object> currentLine = JSON.Parse(saveGame.GetLine()).Result as Dictionary<object, object>;
            if (currentLine == null) continue;

            PackedScene newObjectScene = ResourceLoader.Load(currentLine["Filename"].ToString()) as PackedScene;
            Node newObject = newObjectScene.Instance();
            GetNode(currentLine["Parent"].ToString()).AddChild(newObject);
            
            Vector2 pos = new Vector2();

            foreach (var entry in currentLine){
                string key = entry.Key.ToString();
                if (key == "PosX"){
                    pos.x = (float)entry.Value;
                    continue;
                }
                else if (key == "PosY"){
                    pos.y = (float)entry.Value;
                    continue;
                }
                else if (key == "Filename" || key == "Parent")
                    continue;
                newObject.Set(key, entry.Value);
            }
        }
        saveGame.Close();
    }
}
