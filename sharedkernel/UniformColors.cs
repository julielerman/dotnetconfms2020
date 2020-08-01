using System;
using System.Collections.Generic;
using System.Drawing;

namespace SharedKernel
{
  public class UniformColors
  {
    public UniformColors(Color primary, Color secondary)
    {
      Primary = primary;
      Secondary = secondary;
    }

    public Color Primary { get; private set; }
    public Color Secondary { get; private set; }

    public UniformColors RevisedColors(Color primary, Color secondary)
    {
      var newUniformColors = new UniformColors(
        (primary != null) ? primary : Primary,
        (secondary != null) ? secondary : Secondary
      );
      return newUniformColors;
    }

    public override bool Equals(object obj)
    {
      return obj is UniformColors colors &&
             Primary.Equals(colors.Primary) &&
             Secondary.Equals(colors.Secondary);
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Primary, Secondary);
    }

    public static bool operator ==(UniformColors left, UniformColors right)
    {
      return EqualityComparer<UniformColors>.Default.Equals(left, right);
    }

    public static bool operator !=(UniformColors left, UniformColors right)
    {
      return !(left == right);
    }
  }
}