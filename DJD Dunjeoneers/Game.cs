using Godot;
using System;
using System.Collections.Generic;

public class Game : Node2D{
    public List<Gate> Gates {get; set;} = new List<Gate>();

    private Floor _floor;
    private Player _player;
    private List<List<(Type, int)>> _waveDefinitions = new List<List<(Type, int)>>();
    private int _currentWave = 0;
    private AudioStreamPlayer _bgm;
    private Random _rng = new Random();
    private Globals _globals;

    public Game(){
        var waveOne = new List<(Type, int)>();
        var waveTwo = new List<(Type, int)>();
        var waveThree = new List<(Type, int)>();
        var waveFour = new List<(Type, int)>();
        var waveFive = new List<(Type, int)>();
        // var waveSix = new List<(Type, int)>();

        waveOne.Add((typeof(SlimeBasic), 5));
        
        waveTwo.Add((typeof(SlimeBasic), 15));
        waveTwo.Add((typeof(SlimeFire), 3));

        waveThree.Add((typeof(SlimeBasic), 20));
        waveThree.Add((typeof(SlimeFire), 5));
        
        waveFour.Add((typeof(SlimeBasic), 30));
        waveFour.Add((typeof(SlimeFire), 15));
        
        waveFive.Add((typeof(SlimeBasic), 30));
        waveFive.Add((typeof(SlimeFire), 30));
        
        // waveSix.Add((typeof(Golem), 1));

        _waveDefinitions.Add(waveOne);
        _waveDefinitions.Add(waveTwo);
        _waveDefinitions.Add(waveThree);
        _waveDefinitions.Add(waveFour);
        _waveDefinitions.Add(waveFive);
        // _waveDefinitions.Add(waveSix);
    }

    public override void _Ready(){
        _globals = GetTree().Root.GetNode("Globals") as Globals;
        _floor = GetNode<Floor>("Floor");
        _bgm = GetNode<AudioStreamPlayer>("BGM");

        _globals.Connect("MusicDbChanged", this, "OnMusicDbChanged");
        _globals.Connect("MusicPaused", this, "OnMusicPaused");

        CreateNewGates();
    }

    private void OnMusicDbChanged(float value){
        _bgm.VolumeDb = value;
    }

    private void OnMusicPaused(bool enabled){
        _bgm.StreamPaused = enabled;
    }

    public void CreateNewGates(int wave = 0){
        var spawnedEntities = SpawnEntitiesFromDefinition(_waveDefinitions[wave]);
        var randomizedEntities = RandomizeList(spawnedEntities);
        var gatesEntities = SplitEntityList(randomizedEntities);
        for (int idx = 0; idx < gatesEntities.Count; idx++){
            Gate newGate = new Gate();
            newGate.Position = _floor.GetRandomSpawnLocation();
            newGate.EntitiesToSpawn = gatesEntities[idx];
            newGate.Init();
            GetTree().Root.CallDeferred("add_child", newGate);
            Gates.Add(newGate);
            newGate.Connect("Complete", this, "OnGateComplete");
            if (idx == 0) newGate.ActivateOnReady = true;
            else Gates[idx-1].Connect("Complete", newGate, "Activate"); // TOOD: Make next gate activate when 2/3 of total value reached
        }
    }

    public List<List<Entity>> SplitEntityList(List<Entity> list, int splits = 2){
        var splitList = new List<List<Entity>>();
        int chunkSize = list.Count / splits;
        int leftover = list.Count % splits;
        int c = 0;
        for (int n = 0; n < splits; n++){
            splitList.Add(new List<Entity>());
            for (int i = 0; i < chunkSize + (n < leftover? 0 : 1); i++){
                if (c > list.Count - 1) continue;
                splitList[n].Add(list[c++]);
            }
        }
        return splitList;
    }

    public List<Entity> SpawnEntitiesFromDefinition(List<(Type, int)> definitions){
        var entityList = new List<Entity>();
        foreach ((Type enemyType, int count) definition in definitions){
            for (int idx = 0; idx < definition.count; idx++){
                entityList.Add(Activator.CreateInstance(definition.enemyType, Position, _floor.areaStart, _floor.areaEnd) as Entity);
            }
        }
        return entityList;
    }

    public List<Entity> RandomizeList(List<Entity> list){
        var count = list.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i) {
            var r = _rng.Next(i + 1);
            var tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
        return list;
    }

    public void OnGateComplete(){
        foreach (Gate gate in Gates) if (!gate.IsComplete) return;
        OnWaveComplete();
    }

    // TODO: This should be an "end of wave complete" bonus thing
    public void OnWaveComplete(){
        foreach(Gate gate in Gates){
            // TODO: Spawn loot box or something based on value of gate
            int value = gate.StoredValue;
            gate.QueueFree();
        }
        Gates = new List<Gate>();
        if (_currentWave < _waveDefinitions.Count - 1){
            GD.Print("Creating next wave's gates!");
            CreateNewGates(++_currentWave);
        }
        else GetTree().Root.AddChild(new Golem((_floor.areaStart + _floor.areaEnd) / 2, _floor.areaStart, _floor.areaEnd));
    }

    public void Reset(){
        foreach (Enemy enemy in GetTree().GetNodesInGroup("Enemies")) enemy.QueueFree();
        
        if (IsInstanceValid(_player)) _player.QueueFree();
        _player = new Player();
        _player.Position = (_floor.areaStart + _floor.areaEnd) / 2;
        _player.Connect("Dead", this, "Reset");
        _currentWave = 0;
        GetTree().Root.AddChild(_player);

        foreach (Gate gate in Gates) if (IsInstanceValid(gate)) gate.QueueFree();
        Gates = new List<Gate>();
        CreateNewGates();
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
