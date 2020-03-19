using Godot;
using System;
using System.Collections.Generic;

public class Game : Node2D{
    private Floor floor;
    private Player _player;
    private List<List<(Type, int)>> _waveDefinitions = new List<List<(Type, int)>>();
    private int _currentWave = 0;
    private int _currentWaveGateValue = 0;
    private AudioStreamPlayer bgm;
    private Globals globals;

    public Game(){
        var waveOne = new List<(Type, int)>();
        var waveTwo = new List<(Type, int)>();
        var waveThree = new List<(Type, int)>();
        var waveFour = new List<(Type, int)>();
        var waveFive = new List<(Type, int)>();
        var waveSix = new List<(Type, int)>();

        waveOne.Add((typeof(SlimeBasic), 5));
        
        waveTwo.Add((typeof(SlimeBasic), 15));
        waveTwo.Add((typeof(SlimeFire), 3));

        waveThree.Add((typeof(SlimeBasic), 20));
        waveThree.Add((typeof(SlimeFire), 5));
        
        waveFour.Add((typeof(SlimeBasic), 30));
        waveFour.Add((typeof(SlimeFire), 15));
        
        waveFive.Add((typeof(SlimeBasic), 30));
        waveFive.Add((typeof(SlimeFire), 30));
        
        waveSix.Add((typeof(Golem), 1));

        _waveDefinitions.Add(waveOne);
        _waveDefinitions.Add(waveTwo);
        _waveDefinitions.Add(waveThree);
        _waveDefinitions.Add(waveFour);
        _waveDefinitions.Add(waveFive);
        _waveDefinitions.Add(waveSix);
    }

    public override void _Ready(){
        globals = GetTree().Root.GetNode("Globals") as Globals;
        floor = GetNode<Floor>("Floor");
        bgm = GetNode<AudioStreamPlayer>("BGM");

        globals.Connect("MusicDbChanged", this, "OnMusicDbChanged");
        globals.Connect("MusicPaused", this, "OnMusicPaused");
    }

    private void OnMusicDbChanged(float value){
        bgm.VolumeDb = value;
    }

    private void OnMusicPaused(bool enabled){
        bgm.StreamPaused = enabled;
    }

    public void Reset(){
        foreach (Enemy enemy in GetTree().GetNodesInGroup("Enemies"))
            enemy.QueueFree();
        _currentWave = 0;
        if (IsInstanceValid(_player)) _player.QueueFree();
        _player = new Player();
        _player.Position = (floor.areaStart + floor.areaEnd) / 2;
        _player.Connect("Dead", this, "Reset");
        AddChild(_player);
        SpawnEnemies(_waveDefinitions[_currentWave]);
    }

    public override void _Process(float delta){
        if (GetTree().GetNodesInGroup("Enemies").Count == 0){
            if (++_currentWave < _waveDefinitions.Count){
                SpawnEnemies(_waveDefinitions[_currentWave]);
            }
        }
    }

    public void SpawnEnemies(List<(Type, int)> waveDefinition){
        foreach ((Type enemyType, int count) in waveDefinition){
            for (int i = 0; i < count; i++){
                Vector2 newSpawnPosition = floor.GetRandomSpawnLocation();
                var enemy = Activator.CreateInstance(enemyType, newSpawnPosition, floor.areaStart, floor.areaEnd);
                GetTree().Root.CallDeferred("add_child", enemy);
            }
        }
    }

    public void SaveGame(string fileName = "savegame"){
        File saveGame = new File();
        saveGame.Open(String.Format("user://{0}.dfd", fileName), File.ModeFlags.Write);

        foreach (Node saveNode in GetTree().GetNodesInGroup("Persist")){
            Dictionary<string, object> data = saveNode.Call("GetState") as Dictionary<string, object>;
            saveGame.StoreLine(JSON.Print(data));
        }
        saveGame.Close();
    }

    public void LoadGame(string fileName){
        File saveGame = new File();
        if (!saveGame.FileExists(String.Format("user://{0}.dfd", fileName)))
            return;
        
        // Remove Persist group nodes so they won't be doubled after load
        foreach (Node saveNode in GetTree().GetNodesInGroup("Persist"))
            saveNode.QueueFree();
        
        saveGame.Open(String.Format("user://{0}.dfd", fileName), File.ModeFlags.Read);
        
        while (!saveGame.EofReached()){
            Dictionary<object, object> currentLine = JSON.Parse(saveGame.GetLine()).Result as Dictionary<object, object>;
            if (currentLine == null) continue;

            PackedScene newObjectScene = ResourceLoader.Load(currentLine["Filename"].ToString()) as PackedScene;
            Node newObject = newObjectScene.Instance() as Node;
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
