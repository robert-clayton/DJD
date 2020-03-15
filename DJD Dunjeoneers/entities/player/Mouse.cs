using Godot;
using System;

public class Mouse : Node2D
{
    public override void _Ready()
    {
        // Sets cursor
        var cursor = ResourceLoader.Load("res://assets/sprites/mouse.png");
        Input.SetCustomMouseCursor(cursor);
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
