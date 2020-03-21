using Godot;
using System;
using System.Collections.Generic;

using EnemyGenerator = System.Func<Godot.Vector2, Enemy>;
using Wave = System.Collections.Generic.List<System.ValueTuple<System.Func<Godot.Vector2, Enemy>, int>>;
public class Game : Node2D{
    public List<Gate> Gates {get; set;} = new List<Gate>();

    private Floor _floor;
    private Player _player;
    public List<Wave> WaveDefinitions { get; protected set; }= new List<Wave>();
    public int CurrentWave {get; protected set; } = 0;
    private AudioStreamPlayer _bgm;
    private Random _rng = new Random();
    private Globals _globals;

    private static EnemyGenerator Spawn<T>() where T : Enemy, new(){
        return (position) =>{
            Enemy enemy = new T();
            enemy.Initialize(position);
            return enemy;
        };
    }
    
    public Game(){
        EnemyGenerator
            slime = Spawn<SlimeBasic>(),
            fireSlime = Spawn<SlimeFire>(),
            golem = Spawn<Golem>();

        var waveOne = new Wave {(slime, 5)};
        var waveTwo = new Wave {(slime, 15), (fireSlime, 3)};
        var waveThree = new Wave {(slime, 20), (fireSlime, 5)};
        var waveFour = new Wave {(slime, 30), (fireSlime, 15)};
        var waveFive = new Wave {(slime, 30), (fireSlime, 30)};
        var waveSix = new Wave {(golem, 1)};

        WaveDefinitions.Add(waveOne);
        WaveDefinitions.Add(waveTwo);
        WaveDefinitions.Add(waveThree);
        WaveDefinitions.Add(waveFour);
        WaveDefinitions.Add(waveFive);
        WaveDefinitions.Add(waveSix);
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
        Wave waveDefinition = WaveDefinitions[wave];
        var spawnedEntities = SpawnEntitiesFromDefinition(waveDefinition);
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
            else Gates[idx-1].Connect("ThresholdReached", newGate, "Activate");
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

    public List<Entity> SpawnEntitiesFromDefinition(Wave wave){
        var entityList = new List<Entity>();
        foreach (var (constructEnemy, count) in wave){
            for (int idx = 0; idx < count; idx++){
                // entityList.Add(Activator.CreateInstance(definition.enemyType, Position, _floor.areaStart, _floor.areaEnd) as Entity);
                entityList.Add(constructEnemy(Position));
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
        if (CurrentWave < WaveDefinitions.Count - 1)
            CreateNewGates(++CurrentWave);
        else {
            var boss = new Golem();
            boss.Initialize((_floor.areaStart + _floor.areaEnd) / 2);
            GetTree().Root.AddChild(boss);
        }
    }

    public void Reset(Entity _ = null){
        foreach (Enemy enemy in GetTree().GetNodesInGroup("Enemies")) enemy.QueueFree();
        
        if (IsInstanceValid(_player)) _player.QueueFree();
        _player = new Player();
        _player.Initialize((_floor.areaStart + _floor.areaEnd) / 2);
        _player.Connect("Dead", this, "Reset");
        CurrentWave = 0;
        GetTree().Root.CallDeferred("add_child", _player);

        foreach (Gate gate in Gates) if (IsInstanceValid(gate)) gate.QueueFree();
        Gates = new List<Gate>();
        CreateNewGates();
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
