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


public class FirebaseAuthManager : MonoBehaviour
{
    public FirebaseAuth auth;
    public static FirebaseUser user;
    public static DatabaseReference dbRef;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_InputField emailField;
    [SerializeField] private TMP_InputField pwField;
    [SerializeField] private TMP_InputField nickField;

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
                    message = "이메일 누락";
                    break;
                case AuthError.MissingPassword:
                    message = "패스워드 누락";
                    break;
                case AuthError.WrongPassword:
                    message = "패스워드 틀림";
                    break;
                case AuthError.InvalidEmail:
                    message = "이메일 형식이 옳지 않음";
                    break;
                case AuthError.UserNotFound:
                    message = "아이디가 존재하지 않음";
                    break;
                default:
                    message = "관리자에게 문의 바랍니다";
                    break;
            }

            Debug.LogError(message);
        }
        else
        {
            user = loginTask.Result.User;
            startButton.interactable = true;
        }
    }

    public void Register()
    {
        StartCoroutine(RegisterCor(emailField.text, pwField.text, nickField.text));
    }

    IEnumerator RegisterCor(string email, string password, string userName)
    {
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
                case AuthError.MissingEmail:
                    message = "이메일 누락";
                    break;
                case AuthError.MissingPassword:
                    message = "패스워드 누락";
                    break;
                case AuthError.WeakPassword:
                    message = "패스워드 약함";
                    break;
                case AuthError.EmailAlreadyInUse:
                    message = "중복 이메일";
                    break;
                default:
                    message = "기타 사유. 관리자 문의 바람";
                    break;
            }
            
            Debug.LogError(message);
        }
        else
        {
            user = registerTask.Result.User;

            if (user != null)
            {
                UserProfile profile = new UserProfile { DisplayName = userName };

                Task profileTask = user.UpdateUserProfileAsync(profile);
                yield return new WaitUntil(predicate: () => profileTask.IsCompleted);

                if (profileTask.Exception != null)
                {
                    Debug.LogError("닉네임설정 실패 " + profileTask.Exception);
                    FirebaseException firebaseEx = profileTask.Exception.GetBaseException() as FirebaseException;
                    AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                }
                else
                {
                    startButton.interactable = true;
                }
            }

            UserDataManager.Instance.SetNickname(userName);
        }
    }
}
