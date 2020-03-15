using Godot;
using System;
using System.Collections.Generic;

public class Game : Node2D{
    private Floor floor;
    private Player player;
    List<List<(Type, int)>> waveDefinitions = new List<List<(Type, int)>>();
    private int currentWave = 0;

    public Game(){
        var waveOne = new List<(Type, int)>();
        waveOne.Add((typeof(SlimeBasic), 5));

        var waveTwo = new List<(Type, int)>();
        waveTwo.Add((typeof(SlimeBasic), 15));
        waveTwo.Add((typeof(SlimeFire), 3));

        var waveThree = new List<(Type, int)>();
        waveThree.Add((typeof(SlimeBasic), 20));
        waveThree.Add((typeof(SlimeFire), 5));

        var waveFour = new List<(Type, int)>();
        waveFour.Add((typeof(SlimeBasic), 30));
        waveFour.Add((typeof(SlimeFire), 15));

        var waveFive = new List<(Type, int)>();
        waveFive.Add((typeof(SlimeBasic), 30));
        waveFive.Add((typeof(SlimeFire), 30));

        var waveSix = new List<(Type, int)>();
        waveSix.Add((typeof(Golem), 1));

        waveDefinitions.Add(waveOne);
        waveDefinitions.Add(waveTwo);
        waveDefinitions.Add(waveThree);
        waveDefinitions.Add(waveFour);
        waveDefinitions.Add(waveFive);
        waveDefinitions.Add(waveSix);
    }

    public override void _Ready(){
        floor = GetNode<Floor>("Floor");
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
