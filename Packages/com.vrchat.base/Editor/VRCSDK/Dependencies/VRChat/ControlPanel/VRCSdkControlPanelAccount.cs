using UnityEngine;
using UnityEditor;
using VRC.Core;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;
using VRC.SDKBase;
using VRC.SDKBase.Editor;

public enum TwoFactorType
{
    None,
    TOTP,
    Email,
}

// This file handles the Account tab of the SDK Panel

public partial class VRCSdkControlPanel : EditorWindow
{
    static bool isInitialized = false;
    static string clientInstallPath;
    static bool signingIn = false;
    static string error = null;

    public static bool FutureProofPublishEnabled { get { return UnityEditor.EditorPrefs.GetBool("futureProofPublish", DefaultFutureProofPublishEnabled); } }
    //public static bool DefaultFutureProofPublishEnabled { get { return !SDKClientUtilities.IsInternalSDK(); } }
    public static bool DefaultFutureProofPublishEnabled { get { return false; } }

    static string storedUsername
    {
        get
        {
            return null;
        }
        set
        {
            EditorPrefs.DeleteKey("sdk#username");
        }
    }

    static string storedPassword
    {
        get
        {
            return null;
        }
        set
        {
            EditorPrefs.DeleteKey("sdk#password");
        }
    }

    static string username { get; set; } = null;
    static string password { get; set; } = null;

    public static ApiServerEnvironment ApiEnvironment => serverEnvironment; 
    static ApiServerEnvironment serverEnvironment
    {
        get
        {
            ApiServerEnvironment env = ApiServerEnvironment.Release;
            try
            {
                env = (ApiServerEnvironment)System.Enum.Parse(typeof(ApiServerEnvironment), UnityEditor.EditorPrefs.GetString("VRC_ApiServerEnvironment", env.ToString()));
            }
            catch (System.Exception e)
            {
                Debug.LogError("Invalid server environment name - " + e.ToString());
            }

            return env;
        }
        set
        {
            UnityEditor.EditorPrefs.SetString("VRC_ApiServerEnvironment", value.ToString());

            API.SetApiUrlFromEnvironment(value);
        }
    }

    private void OnEnableAccount()
    {
        entered2faCodeIsInvalid = false;
        warningIconGraphic = Resources.Load("2FAIcons/SDK_Warning_Triangle_icon") as Texture2D;
    }

    public static void RefreshApiUrlSetting()
    {
        // this forces the static api url variable to be reset from the server environment set in editor prefs.
        // needed because the static variable states get cleared when entering / exiting play mode
        ApiServerEnvironment env = serverEnvironment;
        serverEnvironment = env;
    }

    public static void InitAccount()
    {
        if (isInitialized)
            return;

        if (!APIUser.IsLoggedIn && ApiCredentials.Load())
            APIUser.InitialFetchCurrentUser((c) =>
            {
                window.rootVisualElement.Q<IMGUIContainer>().MarkDirtyRepaint();
                var apiUser = c.Model as APIUser;
                AnalyticsSDK.LoggedInUserChanged(apiUser);
                ApiUserPlatforms.Fetch(apiUser.id, null, null);
            }, null);

        clientInstallPath = SDKClientUtilities.GetSavedVRCInstallPath();
        if (string.IsNullOrEmpty(clientInstallPath))
            clientInstallPath = SDKClientUtilities.LoadRegistryVRCInstallPath();

        signingIn = false;
        isInitialized = true;

        ClearContent();
    }

    public static bool OnShowStatus()
    {
        API.SetOnlineMode(true);

        SignIn(false);

        EditorGUILayout.BeginVertical();

        if (APIUser.IsLoggedIn)
        {
            OnCreatorStatusGUI();
        }

        EditorGUILayout.EndVertical();

        return APIUser.IsLoggedIn;
    }

    static bool OnAccountGUI()
    {
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            var newSigningIn = AccountWindowGUI();
            GUILayout.FlexibleSpace();
            return newSigningIn;
        }
    }

    static bool AccountWindowGUI()
    {
        using (new EditorGUILayout.VerticalScope(accountWindowStyle, GUILayout.Height(150), GUILayout.Width(340)))
        {
            EditorGUILayout.LabelField("Account", centeredLabelStyle);
            if (signingIn)
            {
                if (twoFactorAuthenticationEntryType == TwoFactorType.None)
                {
                    EditorGUILayout.LabelField("Signing in as " + username + ".");
                }
                OnTwoFactorAuthenticationGUI(twoFactorAuthenticationEntryType);
                return !signingIn;
            }

            if (APIUser.IsLoggedIn)
            {
                if (Status != "Connected")
                {
                    EditorGUILayout.LabelField(Status);
                }

                OnCreatorStatusGUI();
                
                if (GUILayout.Button("Logout"))
                {
                    storedUsername = username = null;
                    storedPassword = password = null;

                    VRC.Tools.ClearCookies();
                    APIUser.Logout();
                    ClearContent();
                    VRC_EditorTools.GetClearClientMethod().Invoke(null, null);
                }
                return !signingIn;
            }
            
            InitAccount();

            ApiServerEnvironment newEnv = ApiServerEnvironment.Release;
            if (VRCSettings.DisplayAdvancedSettings)
                newEnv = (ApiServerEnvironment)EditorGUILayout.EnumPopup("Use API", serverEnvironment);
            if (serverEnvironment != newEnv)
                serverEnvironment = newEnv;

            username = EditorGUILayout.TextField("Username/Email", username);
            password = EditorGUILayout.PasswordField("Password", password);

            if (GUILayout.Button("Sign In"))
            {
                SignIn(true);
            }

            if (GUILayout.Button("Sign up"))
            {
                Application.OpenURL("https://vrchat.com/register");
            }
            
            return !signingIn;
        }
    }

    static void OnCreatorStatusGUI()
    {
        EditorGUILayout.LabelField("Logged in as:", APIUser.CurrentUser.displayName);

        //if (SDKClientUtilities.IsInternalSDK())
        //    EditorGUILayout.LabelField("Developer Status: ", APIUser.CurrentUser.developerType.ToString());

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("World Creator Status: ", APIUser.CurrentUser.canPublishWorlds ? "Allowed to publish worlds" : "Not yet allowed to publish worlds");
        EditorGUILayout.LabelField("Avatar Creator Status: ", APIUser.CurrentUser.canPublishAvatars ? "Allowed to publish avatars" : "Not yet allowed to publish avatars");
        EditorGUILayout.EndVertical();

        if (!APIUser.CurrentUser.canPublishAllContent)
        {
            if (GUILayout.Button("More Info..."))
            {
                VRCSdkControlPanel.ShowContentPublishPermissionsDialog();
            }
        }


        EditorGUILayout.EndHorizontal();
    }

    void ShowAccount()
    {
        if (VRC.Core.ConfigManager.RemoteConfig.IsInitialized())
        {
            if (VRC.Core.ConfigManager.RemoteConfig.HasKey("sdkUnityVersion"))
            {
                string sdkUnityVersion = VRC.Core.ConfigManager.RemoteConfig.GetString("sdkUnityVersion");
                if (string.IsNullOrEmpty(sdkUnityVersion))
                    EditorGUILayout.LabelField("Could not fetch remote config.");
                else if (Application.unityVersion != sdkUnityVersion)
                {
                    EditorGUILayout.LabelField("Unity Version", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Wrong Unity version. Please use " + sdkUnityVersion);
                }
            }
        }
        else
        {
            VRC.Core.API.SetOnlineMode(true, "vrchat");
            VRC.Core.ConfigManager.RemoteConfig.Init();
        }

        OnAccountGUI();
    }


    private const string TWO_FACTOR_AUTHENTICATION_HELP_URL = "https://docs.vrchat.com/docs/setup-2fa";

    private const string ENTER_2FA_CODE_TITLE_STRING = "Enter a numeric code from your authenticator app.";
    private const string ENTER_2FA_CODE_LABEL_STRING = "Code:";

    private const string ENTER_EMAIL_2FA_CODE_TITLE_STRING = "Check your email for a numeric code.";

    private const string CHECKING_2FA_CODE_STRING = "Checking code...";
    private const string ENTER_2FA_CODE_INVALID_CODE_STRING = "Invalid Code";

    private const string ENTER_2FA_CODE_VERIFY_STRING = "Verify";
    private const string ENTER_2FA_CODE_CANCEL_STRING = "Cancel";
    private const string ENTER_2FA_CODE_HELP_STRING = "Help";

    private const int WARNING_ICON_SIZE = 60;
    private const int WARNING_FONT_HEIGHT = 18;

    static private Texture2D warningIconGraphic;

    static bool entered2faCodeIsInvalid;
    static bool authorizationCodeWasVerified;

    static private int previousAuthenticationCodeLength = 0;
    static bool checkingCode;
    static string authenticationCode = "";

    static System.Action onAuthenticationVerifiedAction;

    static TwoFactorType _twoFactorAuthenticationEntryType = TwoFactorType.None;

    static TwoFactorType twoFactorAuthenticationEntryType
    {
        get
        {
            return _twoFactorAuthenticationEntryType;
        }
        set
        {
            _twoFactorAuthenticationEntryType = value;
            authenticationCode = "";
            if (_twoFactorAuthenticationEntryType == TwoFactorType.None && !authorizationCodeWasVerified)
                Logout();
        }
    }

    static bool IsValidAuthenticationCodeFormat()
    {
        bool isValid2faAuthenticationCode = false;

        if (!string.IsNullOrEmpty(authenticationCode))
        {
            // check if the input is a valid 6-digit numberic code (ignoring spaces)
            Regex rx = new Regex(@"^(\s*\d\s*){6}$", RegexOptions.Compiled);
            MatchCollection matches6DigitCode = rx.Matches(authenticationCode);
            isValid2faAuthenticationCode = (matches6DigitCode.Count == 1);
        }

        return isValid2faAuthenticationCode;
    }

    static bool IsValidRecoveryCodeFormat()
    {
        bool isValid2faRecoveryCode = false;

        if (!string.IsNullOrEmpty(authenticationCode))
        {
            // check if the input is a valid 8-digit alpha-numberic code (format xxxx-xxxx) "-" is optional & ignore any spaces
            // OTP codes also exclude the letters i,l,o and the digit 1 to prevent any confusion
            Regex rx = new Regex(@"^(\s*[a-hj-km-np-zA-HJ-KM-NP-Z02-9]\s*){4}-?(\s*[a-hj-km-np-zA-HJ-KM-NP-Z02-9]\s*){4}$", RegexOptions.Compiled);
            MatchCollection matchesRecoveryCode = rx.Matches(authenticationCode);
            isValid2faRecoveryCode = (matchesRecoveryCode.Count == 1);
        }

        return isValid2faRecoveryCode;
    }

    static void OnTwoFactorAuthenticationGUI(TwoFactorType twoFactorType)
    {
        if (twoFactorType == TwoFactorType.None)
            return;

        const int ENTER_2FA_CODE_BORDER_SIZE = 20;
        const int ENTER_2FA_CODE_BUTTON_WIDTH = 260;
        const int ENTER_2FA_CODE_VERIFY_BUTTON_WIDTH = ENTER_2FA_CODE_BUTTON_WIDTH / 2;
        const int ENTER_2FA_CODE_ENTRY_REGION_WIDTH = 130;
        const int ENTER_2FA_CODE_MIN_WINDOW_WIDTH = ENTER_2FA_CODE_VERIFY_BUTTON_WIDTH + ENTER_2FA_CODE_ENTRY_REGION_WIDTH + (ENTER_2FA_CODE_BORDER_SIZE * 3);

        bool isValidAuthenticationCode = IsValidAuthenticationCodeFormat();


        // Invalid code text
        if (entered2faCodeIsInvalid)
        {
            GUIStyle s = new GUIStyle(EditorStyles.label);
            s.alignment = TextAnchor.UpperLeft;
            s.normal.textColor = Color.red;
            s.fontSize = WARNING_FONT_HEIGHT;
            s.padding = new RectOffset(0, 0, (WARNING_ICON_SIZE - s.fontSize) / 2, 0);
            s.fixedHeight = WARNING_ICON_SIZE;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            var textDimensions = s.CalcSize(new GUIContent(ENTER_2FA_CODE_INVALID_CODE_STRING));
            GUILayout.Label(new GUIContent(warningIconGraphic), GUILayout.Width(WARNING_ICON_SIZE), GUILayout.Height(WARNING_ICON_SIZE));
            EditorGUILayout.LabelField(ENTER_2FA_CODE_INVALID_CODE_STRING, s, GUILayout.Width(textDimensions.x));
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        else if (checkingCode)
        {
            // Display checking code message
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUIStyle s = new GUIStyle(EditorStyles.label);
            s.alignment = TextAnchor.MiddleCenter;
            s.fixedHeight = WARNING_ICON_SIZE;
            EditorGUILayout.LabelField(CHECKING_2FA_CODE_STRING, s, GUILayout.Height(WARNING_ICON_SIZE));
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(ENTER_2FA_CODE_BORDER_SIZE);
            GUILayout.FlexibleSpace();
            GUIStyle titleStyle = new GUIStyle(EditorStyles.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.wordWrap = true;
            string twofactorTitle = "Enter Code";
            switch (twoFactorType)
            {
                case TwoFactorType.TOTP:
                    twofactorTitle = ENTER_2FA_CODE_TITLE_STRING;
                    break;
                case TwoFactorType.Email:
                    twofactorTitle = ENTER_EMAIL_2FA_CODE_TITLE_STRING;
                    break;
            }    
            EditorGUILayout.LabelField(twofactorTitle, titleStyle, GUILayout.Width(ENTER_2FA_CODE_MIN_WINDOW_WIDTH - (2 * ENTER_2FA_CODE_BORDER_SIZE)), GUILayout.Height(WARNING_ICON_SIZE), GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();
            GUILayout.Space(ENTER_2FA_CODE_BORDER_SIZE);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(ENTER_2FA_CODE_BORDER_SIZE);
        GUILayout.FlexibleSpace();
        Vector2 size = EditorStyles.boldLabel.CalcSize(new GUIContent(ENTER_2FA_CODE_LABEL_STRING));
        EditorGUILayout.LabelField(ENTER_2FA_CODE_LABEL_STRING, EditorStyles.boldLabel, GUILayout.MaxWidth(size.x));
        authenticationCode = EditorGUILayout.TextField(authenticationCode);

        // Verify 2FA code button
        if (GUILayout.Button(ENTER_2FA_CODE_VERIFY_STRING, GUILayout.Width(ENTER_2FA_CODE_VERIFY_BUTTON_WIDTH)))
        {
            checkingCode = true;
            string authCodeType = API2FA.TIME_BASED_ONE_TIME_PASSWORD_AUTHENTICATION;
            switch (twoFactorType)
            {
                case TwoFactorType.TOTP:
                    authCodeType = API2FA.TIME_BASED_ONE_TIME_PASSWORD_AUTHENTICATION;
                    break;
                case TwoFactorType.Email:
                    authCodeType = API2FA.EMAIL_BASED_ONE_TIME_PASSWORD_AUTHENTICATION;
                    break;
            }
            APIUser.VerifyTwoFactorAuthCode(authenticationCode, authCodeType, username, password,
                    delegate
                    {
                        // valid 2FA code submitted
                        entered2faCodeIsInvalid = false;
                        authorizationCodeWasVerified = true;
                        checkingCode = false;
                        twoFactorAuthenticationEntryType = TwoFactorType.None;
                        if (null != onAuthenticationVerifiedAction)
                            onAuthenticationVerifiedAction();
                    },
                    delegate
                    {
                        entered2faCodeIsInvalid = true;
                        checkingCode = false;
                    }
                );
        }

        GUILayout.FlexibleSpace();
        GUILayout.Space(ENTER_2FA_CODE_BORDER_SIZE);
        EditorGUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        // after user has entered an invalid code causing the invalid code message to be displayed,
        // edit the code will change it's length meaning it is invalid format, so we can clear the invalid code setting until they resubmit
        if (previousAuthenticationCodeLength != authenticationCode.Length)
        {
            previousAuthenticationCodeLength = authenticationCode.Length;
            entered2faCodeIsInvalid = false;
        }

        GUI.enabled = true;
        GUILayout.FlexibleSpace();
        GUILayout.Space(ENTER_2FA_CODE_BORDER_SIZE);
        EditorGUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        // Two-Factor Authentication Help button
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(ENTER_2FA_CODE_HELP_STRING))
        {
            Application.OpenURL(TWO_FACTOR_AUTHENTICATION_HELP_URL);
        }
        EditorGUILayout.EndHorizontal();

        // Cancel button
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(ENTER_2FA_CODE_CANCEL_STRING))
        {
            twoFactorAuthenticationEntryType = TwoFactorType.None;
            Logout();
        }
        EditorGUILayout.EndHorizontal();
    }

    private static string Status
    {
        get
        {
            if (!APIUser.IsLoggedIn)
                return error == null ? "Please log in." : "Error in authenticating: " + error;
            if (signingIn)
                return "Logging in.";
            else
            {
                if( serverEnvironment == ApiServerEnvironment.Dev )
                    return "Connected to " + serverEnvironment.ToString();
                return "Connected";
            }
        }
    }

    private static void OnAuthenticationCompleted()
    {
        AttemptLogin();
    }

    private static void AttemptLogin()
    {
        APIUser.Login(username, password,
            delegate (ApiModelContainer<APIUser> c)
            {
                APIUser user = c.Model as APIUser;
                if (c.Cookies.ContainsKey("twoFactorAuth"))
                    ApiCredentials.Set(user.username, username, "vrchat", c.Cookies["auth"], c.Cookies["twoFactorAuth"]);
                else if (c.Cookies.ContainsKey("auth"))
                    ApiCredentials.Set(user.username, username, "vrchat", c.Cookies["auth"]);
                else
                    ApiCredentials.SetHumanName(user.username);
                signingIn = false;
                error = null;
                storedUsername = null;
                storedPassword = null;
                AnalyticsSDK.LoggedInUserChanged(user);

                if (!APIUser.CurrentUser.canPublishAllContent)
                {
                    if (UnityEditor.SessionState.GetString("HasShownContentPublishPermissionsDialogForUser", "") != user.id)
                    {
                        UnityEditor.SessionState.SetString("HasShownContentPublishPermissionsDialogForUser", user.id);
                        VRCSdkControlPanel.ShowContentPublishPermissionsDialog();
                    }
                }

                // Fetch platforms that the user can publish to
                ApiUserPlatforms.Fetch(user.id, null, null);
            },
            delegate (ApiModelContainer<APIUser> c)
            {
                Logout();
                error = c.Error;
                EditorUtility.DisplayDialog("Error logging in", error, "OK");
                VRC.Core.Logger.Log("Error logging in: " + error);
            },
            delegate (ApiModelContainer<API2FA> c)
            {
                window.rootVisualElement.Q<IMGUIContainer>().MarkDirtyRepaint();
                if (c.Cookies.ContainsKey("auth"))
                    ApiCredentials.Set(username, username, "vrchat", c.Cookies["auth"]);
                API2FA model2FA = c.Model as API2FA;
                if (model2FA.requiresTwoFactorAuth.Contains(API2FA.TIME_BASED_ONE_TIME_PASSWORD_AUTHENTICATION))
                    twoFactorAuthenticationEntryType = TwoFactorType.TOTP;
                else if (model2FA.requiresTwoFactorAuth.Contains(API2FA.EMAIL_BASED_ONE_TIME_PASSWORD_AUTHENTICATION))
                    twoFactorAuthenticationEntryType = TwoFactorType.Email;
                else
                    twoFactorAuthenticationEntryType = TwoFactorType.None;
                onAuthenticationVerifiedAction = OnAuthenticationCompleted;
            }
        );
    }


    private static object syncObject = new object();
    private static void SignIn(bool explicitAttempt)
    {
        lock (syncObject)
        {
            if (signingIn
                || APIUser.IsLoggedIn
                || (!explicitAttempt && string.IsNullOrEmpty(storedUsername)))
                return;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                EditorUtility.DisplayDialog("Error logging in", "Please enter a valid username and password.", "OK");
                return;
            }

            signingIn = true;
        }

        InitAccount();

        AttemptLogin();
    }

    public static void Logout()
    {
        signingIn = false;
        storedUsername = null;
        storedPassword = null;
        VRC.Tools.ClearCookies();
        APIUser.Logout();
    }

    private void AccountDestroy()
    {
        signingIn = false;
        isInitialized = false;
    }
}
