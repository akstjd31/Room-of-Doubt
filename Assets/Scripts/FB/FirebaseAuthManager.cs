using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Threading.Tasks;
using WebSocketSharp;


public class FirebaseAuthManager : MonoBehaviour
{
    public FirebaseAuth auth;
    public static FirebaseUser user;
    public static DatabaseReference dbRef;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_InputField emailField;
    [SerializeField] private TMP_InputField pwField;
    [SerializeField] private TMP_InputField nickField;
    [SerializeField] private TMP_Text messageText;

    private void Awake()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith
        (
            task =>
            {
                // 만약 유효하다면 인증 데이터 저장
                if (task.Result.Equals(Firebase.DependencyStatus.Available))
                {
                    auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
                    // 추가적인 데이터베이스 레퍼런스 값도 받아오기
                    dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                }
                else
                {
                    Debug.LogError("뭔가 잘못됨!: " + task.Result);
                }
            }
        );
    }

    private void Start()
    {
        startButton.interactable = false;
    }

    public void Login()
    {
        StartCoroutine(LoginCor(emailField.text, pwField.text));
    }

    // 로그인을 위한 코루틴
    IEnumerator LoginCor(string email, string password)
    {
        if (email.IsNullOrEmpty() || password.IsNullOrEmpty()) yield break;

        Task<AuthResult> loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(predicate: () => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            Debug.LogError("다음과 같은 이유로 로그인 실패: " + loginTask.Exception);

            FirebaseException firebaseEx = loginTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "";
            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    message = "이메일을 입력해주세요!";
                    break;
                case AuthError.MissingPassword:
                    message = "패스워드를 입력해주세요!";
                    break;
                case AuthError.WrongPassword:
                    message = "패스워드를 확인해주세요.";
                    break;
                case AuthError.InvalidEmail:
                    message = "이메일 형식이 아닙니다!";
                    break;
                case AuthError.UserNotFound:
                    message = "아이디가 존재하지 않습니다!";
                    break;
                default:
                    message = "관리자에게 문의 바랍니다.";
                    break;
            }

            messageText.color = Color.red;
            messageText.text = message;
        }
        else
        {
            user = loginTask.Result.User;
            string uid = user.UserId;

            var checkTask = dbRef.Child("Users").Child(uid).Child("isLogin").GetValueAsync();
            yield return new WaitUntil(() => checkTask.IsCompleted);

            if (checkTask.Result.Exists && (bool)checkTask.Result.Value == true)
            {
                messageText.color = Color.red;
                messageText.text = "이미 접속 중인 계정입니다.";
                auth.SignOut();
                yield break;
            }

            // 앱이 꺼지거나 연결이 끊기면 false로
            dbRef.Child("Users").Child(uid).Child("isLogin").OnDisconnect().SetValue(false);

            // 현재 로그인 상태를 true로 변경
            yield return dbRef.Child("Users").Child(uid).Child("isLogin").SetValueAsync(true);

            messageText.color = Color.blue;
            messageText.text = "로그인하였습니다! 게임시작 버튼을 눌러주세요.";
            user = loginTask.Result.User;
            startButton.interactable = true;
            UserDataManager.Instance.SetNickname(user.DisplayName);
        }
    }

    public void Register()
    {
        StartCoroutine(RegisterCor(emailField.text, pwField.text, nickField.text));
    }

    IEnumerator RegisterCor(string email, string password, string userName)
    {
        // 닉네임 먼저 체크
        if (string.IsNullOrEmpty(userName))
        {
            messageText.color = Color.red;
            messageText.text = "닉네임을 입력해주세요!";
            yield break;
        }

        // 계정 생성 시도
        Task<AuthResult> registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.Exception != null)
        {
            Debug.LogWarning(message: "실패 사유" + registerTask.Exception);
            FirebaseException firebaseEx = registerTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "회원가입 실패";
            switch (errorCode)
            {
                case AuthError.MissingEmail: message = "이메일을 입력해주세요!"; break;
                case AuthError.MissingPassword: message = "패스워드를 입력해주세요!"; break;
                case AuthError.WeakPassword: message = "패스워드는 최소 6자리 이상으로 작성해주세요!"; break;
                case AuthError.EmailAlreadyInUse: message = "이미 가입된 이메일입니다!"; break;
                default: message = "관리자에게 문의 바랍니다."; break;
            }

            messageText.color = Color.red;
            messageText.text = message;
        }
        else
        {
            // 프로필 설정
            user = registerTask.Result.User;

            if (user != null)
            {
                UserProfile profile = new UserProfile { DisplayName = userName };

                Task profileTask = user.UpdateUserProfileAsync(profile);
                yield return new WaitUntil(predicate: () => profileTask.IsCompleted);

                if (profileTask.Exception != null)
                {
                    messageText.color = Color.red;
                    messageText.text = "닉네임 설정에 실패했습니다!";
                }
                else
                {
                    messageText.color = Color.blue;
                    messageText.text = "회원가입 완료했습니다! 게임시작 버튼을 눌러주세요.";
                    startButton.interactable = true;
                    UserDataManager.Instance.SetNickname(userName);
                }
            }
        }
    }
}
