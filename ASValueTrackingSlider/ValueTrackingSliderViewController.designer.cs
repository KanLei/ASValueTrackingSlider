// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace ASValueTrackingSlider
{
	[Register ("ValueTrackingSliderViewController")]
	partial class ValueTrackingSliderViewController
	{
		[Outlet]
		ASValueTrackingSlider.ValueTrackingSlider.ValueTrackingSlider Slider1 { get; set; }

		[Outlet]
		ASValueTrackingSlider.ValueTrackingSlider.ValueTrackingSlider Slider2 { get; set; }

		[Outlet]
		ASValueTrackingSlider.ValueTrackingSlider.ValueTrackingSlider Slider3 { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (Slider1 != null) {
				Slider1.Dispose ();
				Slider1 = null;
			}

			if (Slider2 != null) {
				Slider2.Dispose ();
				Slider2 = null;
			}

			if (Slider3 != null) {
				Slider3.Dispose ();
				Slider3 = null;
			}
		}
	}
}
