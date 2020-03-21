using Godot;
using System;

public class Floor : Sprite
{
    public int enemiesRemaining = 0;
    [Export] public int enemiesToSpawn = 1;
    private Random _rng = new Random();
    private CollisionShape2D _spawnArea;
    private RectangleShape2D _shape;
    public Vector2 areaStart;
    public Vector2 areaEnd;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready(){
        _spawnArea = GetNode<CollisionShape2D>("SpawnArea/Area");
        _shape = _spawnArea.Shape as RectangleShape2D;
        areaStart = _spawnArea.Position - _shape.Extents;
        areaEnd = _spawnArea.Position + _shape.Extents;
    }

    public Vector2 GetRandomSpawnLocation(){
        return new Vector2(
                2 * (_shape.Extents.x - 15) * (float)_rng.NextDouble() + _spawnArea.Position.x + 15,
                2 * (_shape.Extents.y - 15) * (float)_rng.NextDouble() + _spawnArea.Position.y + 15
            ) - _shape.Extents;
    }
}
