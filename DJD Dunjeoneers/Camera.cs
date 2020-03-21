using Godot;
using System;

public class Camera : Camera2D
{
    private Timer shakeTravelTimer = new Timer();
    private bool shakingTo = false;
    private bool shakingBack = false;
    private bool alreadyShaking = false;
    private Vector2 startPos;
    private Vector2 shakePos;
    private Random rng = new Random();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        AddChild(shakeTravelTimer);
        shakeTravelTimer.WaitTime = 0.1f;
        shakeTravelTimer.OneShot = true;
        shakeTravelTimer.Connect("timeout", this, nameof(OnShakeTravelTimeout));
    }

    public override void _EnterTree(){
        startPos = GlobalPosition;
    }

    public override void _Process(float delta){
        float lerp = 1 - shakeTravelTimer.TimeLeft / shakeTravelTimer.WaitTime;
        if (shakingTo) Position = LerpVector(startPos, shakePos, lerp);
        else if (shakingBack) Position = LerpVector(shakePos, startPos, lerp);
    }

    private Vector2 LerpVector(Vector2 a, Vector2 b, float weight){
        return new Vector2(Mathf.Lerp(a.x, b.x, weight), Mathf.Lerp(a.y, a.y, weight));
    }

    private void OnShakeTravelTimeout(){
        if (shakingTo){
            Position = shakePos;
            shakingTo = false;
            shakingBack = true;
            shakeTravelTimer.Start();
        }
        else{
            Position = startPos;
            shakingBack = false;
            alreadyShaking = false;
        }
    }

    public void ShakeFromDamage(float amplitude){
        if (alreadyShaking) return;
        alreadyShaking = true;
        shakePos = GetNewShakeDirection() * amplitude;
        shakeTravelTimer.Start();
        shakingTo = true;
    }

    public Vector2 GetNewShakeDirection(){
        return new Vector2((float)rng.NextDouble() - .5f, (float)rng.NextDouble() - .5f).Normalized();
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
