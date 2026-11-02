# 초기 개발 환경

- Unity Hub와 Unity 6000.3.27f2를 설치했다.
- Android 모듈과 SDK·NDK 설치 완료. Hub에서 OpenJDK도 이미 설치된 상태임을 확인했다.
- 패키지 설정: URP 17.3.0, Input System 1.20.1. 복원과 C# 컴파일을 완료했고 생성된 lock 파일을 함께 관리한다.
- Git 저장소를 초기화하고 Unity 생성물·개인 설정·설치 로그를 제외했다. 원격은 설정하지 않았다.

## 활성화 후 진행

1. Unity Hub에서 로그인하고 무료 Personal 등 자격에 맞는 라이선스를 활성화한다. 무료 버전도 활성화가 필요하다. https://docs.unity.com/en-us/hub/manage-license
2. Hub에서 이 프로젝트 폴더를 추가하고 6000.3.27f2로 연다.
3. 패키지 복원과 컴파일 결과를 확인한다. Input System 활성화 요청이 나타나면 적용하고 재시작한다.
4. `Zombie Survivor > Create Movement Prototype` 메뉴를 실행한다. 기존 열린 씬을 교체하므로 저장할 변경이 없는 상태에서 실행한다.
5. 생성된 Arena 씬에서 Play를 실행해 WASD/방향키 이동과 추적 카메라를 확인한다. 모바일은 화면 왼쪽 드래그 이동을 사용한다.
6. Android 기기를 선정하고 세로 화면·터치 입력을 빌드로 검증한다.

## 현재 검증 한계

최초 실행은 라이선스 부재(198)로 중단됐으나 사용자 활성화 후 재실행에서 라이선스 인식, 패키지 복원, C# 컴파일, Arena 씬·URP 자산 생성이 완료됐다. Unity는 종료 코드 0으로 정상 종료했다. 실제 플레이와 Android 빌드는 아직 미검증이다.

기준 기기는 사용자 확인이 필요하다. 이후 좀비·전투·3분 생존 구현은 게임 설계 문서 순서를 따른다.

## 이동 플레이 모드 검증

`ZombieSurvivor.Editor.VerifyMovement.Run`을 `-batchmode -nographics -executeMethod`로 실행한다. 이 검증은 완료 후 직접 종료하므로 `-quit`을 함께 전달하지 않는다. 예를 들어 다음 인수를 Unity.exe에 전달한다.

```text
-batchmode -nographics -projectPath C:\dev\zombie-survivor -executeMethod ZombieSurvivor.Editor.VerifyMovement.Run -logFile C:\dev\zombie-survivor\unity-movement.log
```

실제 Input System 키보드 상태 이벤트로 W 이동과 키 해제 후 정지, 맵 경계 제한과 카메라 추적을 확인했다. 최종 결과 `MOVEMENT VERIFICATION PASSED`, 종료 코드 0이다. 검증 과정의 포커스 설정은 배치 검증용으로만 적용한다.

Unity 6000.3.27f2의 에디터 검색 인덱스 초기화에서 `UnityEditor.Search.SearchDatabase`의 `ArgumentOutOfRangeException`이 발생했다. 이 특정 에디터 예외는 로그에 남기고 게임 동작 판정에서 제외한다. 다른 Error·Exception·Assert는 실패로 처리한다. 에디터 예외 자체는 해결되지 않았다.

이 검증은 그래픽 화면·터치·Android 기기 성능 확인을 대체하지 않는다.
