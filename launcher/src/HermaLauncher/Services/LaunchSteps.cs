namespace HermaLauncher.Services;

// Play 진행을 "단계 N/총" 으로 보여주기 위한 매핑(순수 — 단위 테스트 가능).
// 실제로 progress 를 emit 하는 단계 순서: Update→Auth→Java→Packwiz→Launch
// (SessionRefresh 는 무음, Fabric 설치는 Java 단계에 포함). 이 5개를 결정형 진행바의 구간으로 쓴다
// → indeterminate(무한 회전) 대신 단계 경계 + 단계 내 파일 진행률로 바가 실제로 차오른다.
public static class LaunchSteps
{
    public const int Total = 5;

    // 진행 표시용 단계 번호(1..Total). 표시 대상이 아니면 null(Idle/Running/Failed).
    public static int? StepOf(LaunchStage stage) => stage switch
    {
        LaunchStage.Update => 1,
        LaunchStage.Auth => 2,
        LaunchStage.Java or LaunchStage.Fabric => 3,
        LaunchStage.Packwiz => 4,
        LaunchStage.SessionRefresh or LaunchStage.Launch => 5,
        _ => null,
    };
}
