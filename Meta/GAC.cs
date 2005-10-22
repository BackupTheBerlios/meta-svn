// Source: Microsoft KB Article KB317540

/*
SUMMARY
The native code application programming interfaces (APIs) that allow you to interact with the Global Assembly Cache (GAC) are not documented 
in the .NET Framework Software Development Kit (SDK) documentation. 

MORE INFORMATION
CAUTION: Do not use these APIs in your application to perform assembly binds or to test for the presence of assemblies or other run time, 
development, or design-time operations. Only administrative tools and setup programs must use these APIs. If you use the GAC, this directly 
exposes your application to assembly binding fragility or may cause your application to work improperly on future versions of the .NET 
Framework.

The GAC stores assemblies that are shared across all applications on a computer. The actual storage location and structure of the GAC is 
not documented and is subject to change in future versions of the .NET Framework and the Microsoft Windows operating system.

The only supported method to access assemblies in the GAC is through the APIs that are documented in this article.

Most applications do not have to use these APIs because the assembly binding is performed automatically by the common language runtime. 
Only custom setup programs or management tools must use these APIs. Microsoft Windows Installer has native support for installing assemblies
 to the GAC.

For more information about assemblies and the GAC, see the .NET Framework SDK.

Use the GAC API in the following scenarios: 
When you install an assembly to the GAC.
When you remove an assembly from the GAC.
When you export an assembly from the GAC.
When you enumerate assemblies that are available in the GAC.
NOTE: CoInitialize(Ex) must be called before you use any of the functions and interfaces that are described in this specification. 
*/

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;
using System.Collections;
using System.Reflection;

namespace GAC
{		
	[Flags]
	public enum ASM_DISPLAY_FLAGS
	{
		VERSION = 0x1,
		CULTURE = 0x2,
		PUBLIC_KEY_TOKEN = 0x4,
		PUBLIC_KEY = 0x8,
		CUSTOM = 0x10,
		PROCESSORARCHITECTURE = 0x20,
		LANGUAGEID = 0x40
	}

	[Flags]
	public enum ASM_CMP_FLAGS
	{
		NAME = 0x1,
		MAJOR_VERSION = 0x2,
		MINOR_VERSION = 0x4,
		BUILD_NUMBER = 0x8,
		REVISION_NUMBER = 0x10,
		PUBLIC_KEY_TOKEN = 0x20,
		CULTURE = 0x40,
		CUSTOM = 0x80,
		ALL = NAME | MAJOR_VERSION | MINOR_VERSION |
			REVISION_NUMBER | BUILD_NUMBER |
			PUBLIC_KEY_TOKEN | CULTURE | CUSTOM,
		DEFAULT = 0x100
	}

	/// <summary>
	/// The ASM_NAME enumeration property ID describes the valid names of the name-value pairs in an assembly name. 
	/// See the .NET Framework SDK for a description of these properties. 
	/// </summary>
	public enum ASM_NAME
	{
		ASM_NAME_PUBLIC_KEY = 0,
		ASM_NAME_PUBLIC_KEY_TOKEN,
		ASM_NAME_HASH_VALUE,
		ASM_NAME_NAME,
		ASM_NAME_MAJOR_VERSION,
		ASM_NAME_MINOR_VERSION,
		ASM_NAME_BUILD_NUMBER,
		ASM_NAME_REVISION_NUMBER,
		ASM_NAME_CULTURE,
		ASM_NAME_PROCESSOR_ID_ARRAY,
		ASM_NAME_OSINFO_ARRAY,
		ASM_NAME_HASH_ALGID,
		ASM_NAME_ALIAS,
		ASM_NAME_CODEBASE_URL,
		ASM_NAME_CODEBASE_LASTMOD,
		ASM_NAME_NULL_PUBLIC_KEY,
		ASM_NAME_NULL_PUBLIC_KEY_TOKEN,
		ASM_NAME_CUSTOM,
		ASM_NAME_NULL_CUSTOM,                
		ASM_NAME_MVID,
		ASM_NAME_MAX_PARAMS
	} 


	/// <summary>
	/// The ASM_CACHE_FLAGS enumeration contains the following values: 
	/// ASM_CACHE_ZAP - Enumerates the cache of precompiled assemblies by using Ngen.exe.
	/// ASM_CACHE_GAC - Enumerates the GAC.
	/// ASM_CACHE_DOWNLOAD - Enumerates the assemblies that have been downloaded on-demand or that have been shadow-copied.
	/// </summary>
	[Flags]
	public enum ASM_CACHE_FLAGS
	{
		ASM_CACHE_ZAP = 0x1,
		ASM_CACHE_GAC = 0x2,
		ASM_CACHE_DOWNLOAD = 0x4
	}



	public class GlobalAssemblyCache
	{
		public static ArrayList Assemblies
		{
			get
			{
				ArrayList assemblies=new ArrayList();
				assemblies.Add(Assembly.LoadWithPartialName("mscorlib"));

				IAssemblyEnum assemblyEnum=CreateGACEnum();
				IAssemblyName iname; 
				while (GetNextAssembly(assemblyEnum, out iname) == 0)
				{
					try
					{
						string assemblyName=AssemblyName(iname);
						if(assemblyName!="Microsoft.mshtml")
						{
							assemblies.Add(Assembly.LoadWithPartialName(assemblyName));
						}
					}
					catch(Exception e)
					{
					}
				}
				return assemblies;
			}
		}

		private static string AssemblyName(IAssemblyName assemblyName)
		{ 
			AssemblyName name = new AssemblyName();
			name.Name = GetName(assemblyName);
			name.Version = GetVersion(assemblyName);
			name.CultureInfo = GetCulture(assemblyName);
			name.SetPublicKeyToken(GetPublicKeyToken(assemblyName));
			return name.Name;
		}


		/// <summary>
		/// To obtain an instance of the CreateAssemblyEnum API, call the CreateAssemblyNameObject API.
		/// </summary>
		/// <param name="pEnum">Pointer to a memory location that contains the IAssemblyEnum pointer.</param>
		/// <param name="pUnkReserved">Must be null.</param>
		/// <param name="pName">An assembly name that is used to filter the enumeration. Can be null to enumerate all assemblies in the GAC.</param>
		/// <param name="dwFlags">Exactly one bit from the ASM_CACHE_FLAGS enumeration.</param>
		/// <param name="pvReserved">Must be NULL.</param>
		[DllImport("fusion.dll", SetLastError=true, PreserveSig=false)]
		static extern void CreateAssemblyEnum(out IAssemblyEnum pEnum, IntPtr pUnkReserved, IAssemblyName pName,
			ASM_CACHE_FLAGS dwFlags, IntPtr pvReserved);




		public static  String GetDisplayName(IAssemblyName name, ASM_DISPLAY_FLAGS which)
		{
			uint bufferSize = 255;
			StringBuilder buffer = new StringBuilder((int) bufferSize);
			name.GetDisplayName(buffer, ref bufferSize, which);
			return buffer.ToString();
		}

		public static  String GetName(IAssemblyName name)
		{
			uint bufferSize = 255;
			StringBuilder buffer = new StringBuilder((int) bufferSize);
			name.GetName(ref bufferSize, buffer);
			return buffer.ToString();
		}

		public static Version GetVersion(IAssemblyName name)
		{
			uint major;
			uint minor;
			name.GetVersion(out major, out minor);
			return new Version((int)major>>16, (int)major&0xFFFF, (int)minor>>16, (int)minor&0xFFFF);
		}

		public static byte[] GetPublicKeyToken(IAssemblyName name)
		{
			byte[] result = new byte[8];
			uint bufferSize = 8;
			IntPtr buffer = Marshal.AllocHGlobal((int) bufferSize);
			name.GetProperty(ASM_NAME.ASM_NAME_PUBLIC_KEY_TOKEN, buffer, ref bufferSize);
			for (int i = 0; i < 8; i++)
				result[i] = Marshal.ReadByte(buffer, i);
			Marshal.FreeHGlobal(buffer);
			return result;
		}

		public static byte[] GetPublicKey(IAssemblyName name)
		{
			uint bufferSize = 512;
			IntPtr buffer = Marshal.AllocHGlobal((int) bufferSize);
			name.GetProperty(ASM_NAME.ASM_NAME_PUBLIC_KEY, buffer, ref bufferSize);
			byte[] result = new byte[bufferSize];
			for (int i = 0; i < bufferSize; i++)
				result[i] = Marshal.ReadByte(buffer, i);
			Marshal.FreeHGlobal(buffer);
			return result;
		}

		public static CultureInfo GetCulture(IAssemblyName name)
		{
			uint bufferSize = 255;
			IntPtr buffer = Marshal.AllocHGlobal((int) bufferSize);
			name.GetProperty(ASM_NAME.ASM_NAME_CULTURE, buffer, ref bufferSize);
			string result = Marshal.PtrToStringAuto(buffer);
			Marshal.FreeHGlobal(buffer);
			return new CultureInfo(result);
		}

		public static IAssemblyEnum CreateGACEnum()
		{
			IAssemblyEnum ae;

			GlobalAssemblyCache.CreateAssemblyEnum(out ae, (IntPtr)0, null, ASM_CACHE_FLAGS.ASM_CACHE_GAC, (IntPtr)0);

			return ae;
		}

		/// <summary>
		/// Get the next assembly name in the current enumerator or fail
		/// </summary>
		/// <returns>0 if the enumeration is not at its end</returns>
		public static int GetNextAssembly(IAssemblyEnum enumerator, out IAssemblyName name)
		{
			return enumerator.GetNextAssembly((IntPtr)0, out name, 0);
		}

	}


//	[ComImport, Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae"),
//	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
//	public interface IAssemblyCache
//	{
//	}


	[ComImport, Guid("CD193BC0-B4BC-11d2-9833-00C04FC31D2E"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAssemblyName
	{
		[PreserveSig]
		int SetProperty(ASM_NAME PropertyId,IntPtr pvProperty,uint cbProperty);

		[PreserveSig]
		int GetProperty(ASM_NAME PropertyId,IntPtr pvProperty,ref uint pcbProperty);

		[PreserveSig]
		int Finalize();

		[PreserveSig]
		int GetDisplayName([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder szDisplayName,
			ref uint pccDisplayName,ASM_DISPLAY_FLAGS dwDisplayFlags);


		[PreserveSig]
		int BindToObject(ref Guid refIID,[MarshalAs(UnmanagedType.IUnknown)] object pUnkSink,
			[MarshalAs(UnmanagedType.IUnknown)] object pUnkContext,[MarshalAs(UnmanagedType.LPWStr)] string szCodeBase,
			long llFlags,IntPtr pvReserved,uint cbReserved,out IntPtr ppv);

		[PreserveSig]
		int GetName(ref uint lpcwBuffer,[Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzName);

		[PreserveSig]
		int GetVersion(out uint pdwVersionHi,out uint pdwVersionLow);

		[PreserveSig]
		int IsEqual(IAssemblyName pName,ASM_CMP_FLAGS dwCmpFlags);

		[PreserveSig]
		int Clone(out IAssemblyName pName);
	}

	[ComImport, Guid("21b8916c-f28e-11d2-a473-00c04f8ef448"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAssemblyEnum
	{
		[PreserveSig()]
		int GetNextAssembly(IntPtr pvReserved,out IAssemblyName ppName,uint dwFlags);

		[PreserveSig()]
		int Reset();

		[PreserveSig()]
		int Clone(out IAssemblyEnum ppEnum);
	}
}
