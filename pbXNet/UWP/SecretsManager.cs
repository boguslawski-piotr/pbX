#if WINDOWS_UWP

using System;

namespace pbXNet
{
	// TODO: SecretsManager DOAuth UWP

	public partial class SecretsManager : ISecretsManager
	{

// Documentation:
// https://docs.microsoft.com/en-us/uwp/api/windows.security.credentials.ui.userconsentverifier

		public DOAuthentication AvailableDOAuthentication => DOAuthentication.None;

		public void Initialize(object param)
		{
		}

		public bool StartDOAuthentication(string msg, Action Succes, Action<string, bool> ErrorOrHint)
		{
			return false;
		}

		public bool CanDOAuthenticationBeCanceled()
		{
			return false;
		}

		public void CancelDOAuthentication()
		{
		}
	}
}

#endif
