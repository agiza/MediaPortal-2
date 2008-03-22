using System;
using System.Collections.Generic;
using System.Text;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;

namespace Presentation.SkinEngine
{
  public interface ITextureAsset : IAsset
  {
    Texture Texture { get;}
    void Allocate();
    void KeepAlive();
  }
}
