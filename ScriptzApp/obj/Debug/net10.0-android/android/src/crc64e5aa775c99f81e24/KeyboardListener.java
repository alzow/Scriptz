package crc64e5aa775c99f81e24;


public class KeyboardListener
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		android.view.ViewTreeObserver.OnGlobalLayoutListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onGlobalLayout:()V:GetOnGlobalLayoutHandler:Android.Views.ViewTreeObserver+IOnGlobalLayoutListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"";
		mono.android.Runtime.register ("MPowerKit.Popups.KeyboardListener, MPowerKit.Popups", KeyboardListener.class, __md_methods);
	}

	public KeyboardListener ()
	{
		super ();
		if (getClass () == KeyboardListener.class) {
			mono.android.TypeManager.Activate ("MPowerKit.Popups.KeyboardListener, MPowerKit.Popups", "", this, new java.lang.Object[] {  });
		}
	}

	public KeyboardListener (android.view.ViewGroup p0)
	{
		super ();
		if (getClass () == KeyboardListener.class) {
			mono.android.TypeManager.Activate ("MPowerKit.Popups.KeyboardListener, MPowerKit.Popups", "Android.Views.ViewGroup, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public void onGlobalLayout ()
	{
		n_onGlobalLayout ();
	}

	private native void n_onGlobalLayout ();

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
