using Godot;

/// <summary>
/// 向日葵
/// </summary>
public partial class SunFlower : FunctionPlant
{
    [Export]
    public PackedScene sunScene;
    private double _sunProductionTimer = 0.0;
    private double SunProductionInterval = 5.0; // 每5秒产生一次阳光

    private bool isGrowth;

    private bool isTimer;
    public override void _Ready()
    {
        base._Ready();
        isGrowth = false;
        isTimer = false;
    }

    public override void Hooks(double delta)
    {
        if (!isTimer)
        {
            if (isGrowth)
            {
                SunProductionInterval = _rng.RandfRange(23f, 23.5f);
            }
            else
            {
                SunProductionInterval = _rng.RandfRange(3f, 12.5f);
            }
        }
        isTimer = true;
        // 计时器增加
        _sunProductionTimer += delta;

        // 如果达到生产间隔，生成阳光
        if (_sunProductionTimer >= SunProductionInterval)
        {
            ProduceSun();
            _sunProductionTimer = 0.0; // 重置计时器
            isTimer = false;
        }


    }

    /// <summary>
    /// 生成阳光的方法
    /// </summary>
    private void ProduceSun()
    {
        GD.Print("Spawn a sun");
        var sun = sunScene.Instantiate() as Sun;

        // 设置阳光位置在向日葵上方
        sun.Position = this.Position + new Vector2(0, -20);

        sun.suns = 25;

        // 将阳光添加到场景中
        GetParent().AddChild(sun);
    }

}