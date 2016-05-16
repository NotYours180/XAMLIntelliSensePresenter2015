//-----------------------------------------------------------------------
// <copyright file="GreyscaleEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//  Greyscale effect.
// </summary>
namespace Microsoft.VisualStudio.XAMLIntellisensePresenter 
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Effects;

    /// <summary>
    /// Greyscale effect.
    /// </summary>
    public class GreyscaleEffect : ShaderEffect
    {
        /// <summary>
        /// Dependency property for Input.
        /// </summary>
        public static readonly DependencyProperty InputProperty =
            ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(GreyscaleEffect), 0 /* assigned to sampler register S0 */);

        /// <summary>
        /// PixelShader for this effect.
        /// </summary>
        private static PixelShader pixelShader;

        /// <summary>
        /// Static constructor - Create a PixelShader for all GreyscaleEffect instances. 
        /// </summary>
        static GreyscaleEffect()
        {
            pixelShader = new PixelShader();
            pixelShader.UriSource = new Uri("pack://application:,,,/Microsoft.VisualStudio.XAMLIntellisensePresenter;component/CustomEffects/Bytecode/Greyscale.ps");
            pixelShader.Freeze();
        }

        /// <summary>
        /// Constructor - Assign the PixelShader property and set the shader parameters to default values.
        /// </summary>
        public GreyscaleEffect()
        {
            this.PixelShader = pixelShader;
            UpdateShaderValue(InputProperty);
        }

        /// <summary>
        /// Gets or sets Input properties. 
        /// </summary>
        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }
    }
}
