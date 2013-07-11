/*
* MonochromeEffect.cs
* http://www.transcendingdigital.com
*
*
* Copyright (c) 2013 Transcending Digital LLC
* This file is part of a Kinect For Windows green screen experience 
* initially deployed at Maker Faire Detroit 2013. This application
* works in concert with HTML5 tablet applications for e-mailing photos.
*
* Permission is hereby granted, free of charge, to any person
* obtaining a copy of this software and associated documentation
* files (the "Software"), to deal in the Software without
* restriction, including without limitation the rights to use,
* copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the
* Software is furnished to do so, subject to the following
* conditions:
*
* The above copyright notice and this permission notice shall be
* included in all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
* OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
* NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
* HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
* WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
* FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
* OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;

namespace KinectGreenScreen.com.transcendingdigital.effects
{
    /// <summary>
    /// This particular class is a fun custom effect HLSL shader
    /// implementation.  If you are curious how to do these you should
    /// reference the great Shazzam shader editor project.
    /// </summary>
    public class MonochromeEffect : ShaderEffect
    {
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(MonochromeEffect), 0);
        public static readonly DependencyProperty FilterColorProperty = DependencyProperty.Register("FilterColor", typeof(Color), typeof(MonochromeEffect), new UIPropertyMetadata(Color.FromArgb(255, 255, 255, 0), PixelShaderConstantCallback(0)));
        public static readonly DependencyProperty ContrastProperty = DependencyProperty.Register("Contrast", typeof(double), typeof(MonochromeEffect), new UIPropertyMetadata(((double)(1.5D)), PixelShaderConstantCallback(1)));
        public MonochromeEffect()
        {
            PixelShader pixelShader = new PixelShader();
            pixelShader.UriSource = new Uri("pack://application:,,,/com/transcendingdigital/effects/MonochromeShader.ps");
            this.PixelShader = pixelShader;

            this.UpdateShaderValue(InputProperty);
            this.UpdateShaderValue(FilterColorProperty);
            this.UpdateShaderValue(ContrastProperty);
        }
        public Brush Input
        {
            get
            {
                return ((Brush)(this.GetValue(InputProperty)));
            }
            set
            {
                this.SetValue(InputProperty, value);
            }
        }
        /// <summary>The color used to tint the input.</summary>
        public Color FilterColor
        {
            get
            {
                return ((Color)(this.GetValue(FilterColorProperty)));
            }
            set
            {
                this.SetValue(FilterColorProperty, value);
            }
        }
        /// <summary>The contrast multiplier.</summary>
        public double Contrast
        {
            get
            {
                return ((double)(this.GetValue(ContrastProperty)));
            }
            set
            {
                this.SetValue(ContrastProperty, value);
            }
        }
    }
}
