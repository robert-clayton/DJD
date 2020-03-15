using Godot;
using System;

public class Golem : Boss{

    public Golem(Vector2 _position, Vector2 _moveAreaStart, Vector2 _moveAreaEnd) : base(_position, _moveAreaStart, _moveAreaEnd){
        sprite.Frames = ResourceLoader.Load("res://entities/enemies/bosses/golem/Golem.tres") as SpriteFrames;
        ChangeState(EEnemyState.STATE_IDLE);
        maxVelocity = 5;
    }
}
