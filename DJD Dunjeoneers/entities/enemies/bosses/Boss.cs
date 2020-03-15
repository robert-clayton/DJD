using Godot;
using System;

public class Boss : Enemy{
    protected Tween tween = new Tween();
    protected Timer lurchTimer = new Timer();
    protected Vector2 lurchTarget = default(Vector2);
    
    public Boss(Vector2 _position, Vector2 _moveAreaStart, Vector2 _moveAreaEnd) : base(_position, _moveAreaStart, _moveAreaEnd, size: 24){
        curHealth = 1000f;
        maxHealth = 1000f;
        knockbackResistance = .7f;

        lurchTimer.WaitTime = 2f;
        lurchTimer.OneShot = true;
        lurchTimer.Connect("timeout", this, "FindLurchTarget");
        AddChild(tween);
        AddChild(lurchTimer);
        
    }

    public override void _Ready(){
        base._Ready();
        lurchTimer.Start();
    }

    public override void _PhysicsProcess(float delta){
        ProcessKnockback(delta);
    }

    public void FindLurchTarget(){
        var playerList = GetTree().GetNodesInGroup("Players");
        if (playerList.Count > 0){
            Player player = playerList[0] as Player;
            var lurchTime = 1f;
            if (Position.DistanceTo(player.GlobalPosition) > lurchTime * 30f){
                lurchTarget = Position + Position.DirectionTo(player.GlobalPosition) * 30f;
            } else {
                lurchTarget = player.GlobalPosition;
            }
            tween.Remove(this, "global_position");
            tween.InterpolateProperty(this, "global_position", Position, lurchTarget, lurchTime, easeType: Tween.EaseType.Out);
            tween.Start();
            lurchTimer.Start();
        }

    }
}
