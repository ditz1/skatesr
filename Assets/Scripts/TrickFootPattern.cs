using UnityEngine;

// Base class for trick foot patterns
public abstract class TrickFootPattern
{
    public static float base_y_offset = 0.02f;
    // Evaluate foot position offset at a given progress (0 to 1)
    public abstract Vector3 EvaluateLeftFoot(float progress);
    public abstract Vector3 EvaluateRightFoot(float progress);
    
    // Helper function for smooth curves
    protected float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }
    
    protected float EaseInOut(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }
    
    protected float EaseOut(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
    
    protected float EaseIn(float t)
    {
        return t * t * t;
    }
}

// KICKFLIP PATTERN
// Front foot: slides up and out to flick
// Back foot: pops down then lifts up
public class KickflipPattern : TrickFootPattern
{
    private float popHeight = 0.15f;
    private float flickDistance = 0.5f;
    private float liftHeight = 0.3f;
    
    public override Vector3 EvaluateRightFoot(float progress)
    {
        // Left foot is typically the front foot for regular stance
        // Slide up the board, then flick out
        Vector3 offset = Vector3.zero;
        offset.y += base_y_offset;
        
        if (progress < 0.3f)
        {
            // Slide phase (0 to 0.3)
            float slideProgress = progress / 0.3f;
            offset.z = slideProgress * 0.15f; // Slide forward
            offset.y = base_y_offset + slideProgress * 0.05f; // Slight lift
        }
        else if (progress < 0.5f)
        {
            // Flick phase (0.3 to 0.5)
            float flickProgress = (progress - 0.3f) / 0.2f;
            offset.z = 0.15f; // Maintain forward position
            offset.x = -flickProgress * flickDistance; // Flick left
            offset.y = base_y_offset + flickProgress * 0.1f; // Lift during flick
        }
        else
        {
            // Recovery phase (0.5 to 1.0)
            float recoveryProgress = (progress - 0.5f) / 0.5f;
            offset.z = Mathf.Lerp(0.15f, 0f, EaseOut(recoveryProgress));
            offset.x = Mathf.Lerp(-flickDistance, 0f, EaseOut(recoveryProgress));
            offset.y = Mathf.Lerp(0.15f, liftHeight * (1f - recoveryProgress), recoveryProgress);
        }
        
        return offset;
    }
    
    public override Vector3 EvaluateLeftFoot(float progress)
    {
        // Right foot is typically the back foot for regular stance
        // Pop down, then lift up
        Vector3 offset = Vector3.zero;
        
        if (progress < 0.2f)
        {
            // Pop phase (0 to 0.2)
            float popProgress = progress / 0.2f;
            offset.y = -popHeight * Mathf.Sin(popProgress * Mathf.PI); // Down and back up
        }
        else
        {
            // Lift phase (0.2 to 1.0)
            float liftProgress = (progress - 0.2f) / 0.8f;
            offset.y = liftHeight * Mathf.Sin(liftProgress * Mathf.PI * 0.5f);
        }
        
        return offset;
    }
}

// HEELFLIP PATTERN
// Front foot: slides up and flicks heel-side (opposite of kickflip)
// Back foot: same pop motion as kickflip
public class HeelflipPattern : TrickFootPattern
{
    private float popHeight = 0.15f;
    private float flickDistance = 0.5f;
    private float liftHeight = 0.3f;
    
    public override Vector3 EvaluateRightFoot(float progress)
    {
        // Heelflip flicks in the opposite direction
        Vector3 offset = Vector3.zero;
        
        if (progress < 0.3f)
        {
            float slideProgress = progress / 0.3f;
            offset.z = slideProgress * 0.15f;
            offset.y = base_y_offset + slideProgress * 0.05f;
        }
        else if (progress < 0.5f)
        {
            float flickProgress = (progress - 0.3f) / 0.2f;
            offset.z = 0.15f;
            offset.x = flickProgress * flickDistance; // Flick right (opposite of kickflip)
            offset.y = base_y_offset + flickProgress * 0.1f;
        }
        else
        {
            float recoveryProgress = (progress - 0.5f) / 0.5f;
            offset.z = Mathf.Lerp(0.15f, 0f, EaseOut(recoveryProgress));
            offset.x = Mathf.Lerp(flickDistance, 0f, EaseOut(recoveryProgress));
            offset.y = Mathf.Lerp(0.15f, liftHeight * (1f - recoveryProgress), recoveryProgress);
        }
        
        return offset;
    }
    
    public override Vector3 EvaluateLeftFoot(float progress)
    {
        // Same pop as kickflip
        Vector3 offset = Vector3.zero;
        
        if (progress < 0.2f)
        {
            float popProgress = progress / 0.2f;
            offset.y = -popHeight * Mathf.Sin(popProgress * Mathf.PI);
        }
        else
        {
            float liftProgress = (progress - 0.2f) / 0.8f;
            offset.y = liftHeight * Mathf.Sin(liftProgress * Mathf.PI * 0.5f);
        }
        
        return offset;
    }
}

// SHUVIT PATTERN  
// Front foot: stays relatively centered, guides the board
// Back foot: scoops back and around
public class ShuvitPattern : TrickFootPattern
{
    private float scoopDistance = 0.25f;
    private float liftHeight = 0.25f;
    
    public override Vector3 EvaluateRightFoot(float progress)
    {
        // Front foot stays more stable, just lifts
        Vector3 offset = Vector3.zero;
        
        // Slight backward movement to give board space
        if (progress < 0.4f)
        {
            float moveProgress = progress / 0.4f;
            offset.z = -0.05f * moveProgress;
        }
        else
        {
            offset.z = -0.05f * (1f - ((progress - 0.4f) / 0.6f));
        }
        
        // Lift throughout
        offset.y = liftHeight * Mathf.Sin(progress * Mathf.PI);
        
        return offset;
    }
    
    public override Vector3 EvaluateLeftFoot(float progress)
    {
        // Back foot scoops - this is the key shuvit movement
        return EvaluateShuvitBackFoot(progress);
    }
    
    // Reusable shuvit back foot movement
    public static Vector3 EvaluateShuvitBackFoot(float progress)
    {
        Vector3 offset = Vector3.zero;
        float scoopDistance = 0.25f;
        float liftHeight = 0.25f;
        
        if (progress < 0.3f)
        {
            // Scoop back phase
            float scoopProgress = progress / 0.3f;
            offset.z = -scoopDistance * scoopProgress;
            offset.y = base_y_offset + 0.05f * scoopProgress;
        }
        else if (progress < 0.6f)
        {
            // Scoop around phase
            float aroundProgress = (progress - 0.3f) / 0.3f;
            offset.z = -scoopDistance * (1f - aroundProgress * 0.5f);
            offset.x = scoopDistance * 0.3f * Mathf.Sin(aroundProgress * Mathf.PI);
            offset.y = base_y_offset + liftHeight * aroundProgress;
        }
        else
        {
            // Recovery phase
            float recoveryProgress = (progress - 0.6f) / 0.4f;
            offset.z = -scoopDistance * 0.5f * (1f - recoveryProgress);
            offset.y = liftHeight * (1f - recoveryProgress);
        }
        
        return offset;
    }
}

// VARIAL KICKFLIP PATTERN
// Combines kickflip front foot with shuvit back foot
public class VarialKickflipPattern : TrickFootPattern
{
    private KickflipPattern kickflip = new KickflipPattern();
    
    public override Vector3 EvaluateLeftFoot(float progress)
    {
        // Use kickflip front foot motion
        return kickflip.EvaluateLeftFoot(progress);
    }
    
    public override Vector3 EvaluateRightFoot(float progress)
    {
        // Use shuvit back foot motion
        return ShuvitPattern.EvaluateShuvitBackFoot(progress);
    }
}

// VARIAL HEELFLIP PATTERN
// Combines heelflip front foot with shuvit back foot (opposite direction)
public class VarialHeelflipPattern : TrickFootPattern
{
    private HeelflipPattern heelflip = new HeelflipPattern();
    
    public override Vector3 EvaluateLeftFoot(float progress)
    {
        // Use heelflip front foot motion
        return heelflip.EvaluateLeftFoot(progress);
    }
    
    public override Vector3 EvaluateRightFoot(float progress)
    {
        // Use shuvit back foot motion, but mirrored
        Vector3 offset = ShuvitPattern.EvaluateShuvitBackFoot(progress);
        offset.x = -offset.x; // Mirror the scoop direction
        return offset;
    }
}

// MANUAL PATTERN
// Both feet adjust to maintain balance on tail or nose
public class ManualPattern : TrickFootPattern
{
    private bool isNoseManual;
    
    public ManualPattern(bool noseManual = false)
    {
        isNoseManual = noseManual;
    }
    
    public override Vector3 EvaluateLeftFoot(float progress)
    {
        Vector3 offset = Vector3.zero;
        
        if (isNoseManual)
        {
            // Nose manual - front foot is lower, back foot is higher
            offset.z = 0.1f;
            offset.y = -0.05f;
        }
        else
        {
            // Tail manual - front foot is higher, back foot is lower
            offset.z = -0.05f;
            offset.y = 0.1f;
        }
        
        return offset;
    }
    
    public override Vector3 EvaluateRightFoot(float progress)
    {
        Vector3 offset = Vector3.zero;
        
        if (isNoseManual)
        {
            // Nose manual
            offset.z = 0.05f;
            offset.y = 0.15f;
        }
        else
        {
            // Tail manual
            offset.z = -0.1f;
            offset.y = -0.05f;
        }
        
        return offset;
    }
}

// GRIND PATTERN
// Feet stay planted but adjust based on grind type
public class GrindPattern : TrickFootPattern
{
    private GrindType grindType;
    
    public enum GrindType
    {
        FiftyFifty,     // Both trucks
        FiveO,          // Back truck only
        Nosegrind,      // Front truck only
        Crooked,        // Front truck, nose down
        Smith           // Back truck, tail down
    }
    
    public GrindPattern(GrindType type = GrindType.FiftyFifty)
    {
        grindType = type;
    }
    
    public override Vector3 EvaluateRightFoot(float progress)
    {
        Vector3 offset = Vector3.zero;
        
        switch (grindType)
        {
            case GrindType.FiftyFifty:
                // Centered position
                offset = Vector3.zero;
                break;
                
            case GrindType.Nosegrind:
                // Weight forward
                offset.z = 0.08f;
                offset.y = -0.03f;
                break;
                
            case GrindType.Crooked:
                // Front foot forward and down
                offset.z = 0.12f;
                offset.y = -0.05f;
                break;
                
            case GrindType.FiveO:
                // Weight back
                offset.z = -0.05f;
                offset.y = 0.05f;
                break;
                
            case GrindType.Smith:
                // Weight back, front foot up
                offset.z = -0.03f;
                offset.y = 0.08f;
                break;
        }
        
        return offset;
    }
    
    public override Vector3 EvaluateLeftFoot(float progress)
    {
        Vector3 offset = Vector3.zero;
        
        switch (grindType)
        {
            case GrindType.FiftyFifty:
                offset = Vector3.zero;
                break;
                
            case GrindType.Nosegrind:
                offset.z = 0.03f;
                offset.y = 0.08f;
                break;
                
            case GrindType.Crooked:
                offset.z = 0.05f;
                offset.y = 0.1f;
                break;
                
            case GrindType.FiveO:
                offset.z = -0.08f;
                offset.y = -0.03f;
                break;
                
            case GrindType.Smith:
                offset.z = -0.12f;
                offset.y = -0.05f;
                break;
        }
        
        return offset;
    }
}