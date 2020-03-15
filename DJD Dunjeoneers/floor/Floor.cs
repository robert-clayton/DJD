using Godot;
using System;

public class Floor : Sprite
{
    public int enemiesRemaining = 0;
    [Export] public int enemiesToSpawn = 1;
    private Random rng = new Random();
    private CollisionShape2D spawnArea;
    private RectangleShape2D shape;
    public Vector2 areaStart;
    public Vector2 areaEnd;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready(){
        spawnArea = GetNode<CollisionShape2D>("SpawnArea/Area");
        shape = (RectangleShape2D)spawnArea.Shape;
        areaStart = spawnArea.Position - shape.Extents;
        areaEnd = spawnArea.Position + shape.Extents;
    }

    public Vector2 GetRandomSpawnLocation(){
        return new Vector2(
                2 * (shape.Extents.x - 15) * (float)rng.NextDouble() + spawnArea.Position.x + 15,
                2 * (shape.Extents.y - 15) * (float)rng.NextDouble() + spawnArea.Position.y + 15
            ) - shape.Extents;
    }
}
