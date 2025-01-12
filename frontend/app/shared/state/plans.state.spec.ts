/*
 * Squidex Headless CMS
 *
 * @license
 * Copyright (c) Squidex UG (haftungsbeschränkt). All rights reserved.
 */

import { DialogService, PlanDto, PlansService, PlansState, versioned } from '@app/shared/internal';
import { of, throwError } from 'rxjs';
import { onErrorResumeNext } from 'rxjs/operators';
import { IMock, It, Mock, Times } from 'typemoq';
import { TestValues } from './_test-helpers';

describe('PlansState', () => {
    const {
        app,
        appsState,
        authService,
        creator,
        newVersion,
        version
    } = TestValues;

    const oldPlans = {
        currentPlanId: 'id1',
        planOwner: creator,
        plans: [
            new PlanDto('id1', 'name1', '100€', undefined, 'id1_yearly', '200€', undefined, 1, 1, 1, 1),
            new PlanDto('id2', 'name2', '400€', undefined, 'id2_yearly', '800€', undefined, 2, 2, 2, 2)
        ],
        hasPortal: true
    };

    let dialogs: IMock<DialogService>;
    let plansService: IMock<PlansService>;
    let plansState: PlansState;

    beforeEach(() => {
        dialogs = Mock.ofType<DialogService>();

        plansService = Mock.ofType<PlansService>();
        plansState = new PlansState(appsState.object, authService.object, dialogs.object, plansService.object);
    });

    afterEach(() => {
        plansService.verifyAll();
    });

    describe('Loading', () => {
        it('should load plans', () => {
            plansService.setup(x => x.getPlans(app))
                .returns(() => of(versioned(version, oldPlans))).verifiable();

            plansState.load().subscribe();

            expect(plansState.snapshot.plans).toEqual([
                { isSelected: true,  isYearlySelected: false, plan: oldPlans.plans[0] },
                { isSelected: false, isYearlySelected: false, plan: oldPlans.plans[1] }
            ]);
            expect(plansState.snapshot.isOwner).toBeFalsy();
            expect(plansState.snapshot.isLoaded).toBeTruthy();
            expect(plansState.snapshot.hasPortal).toBeTruthy();
            expect(plansState.snapshot.version).toEqual(version);

            dialogs.verify(x => x.notifyInfo(It.isAnyString()), Times.never());
        });

        it('should load plans with overriden id', () => {
            plansService.setup(x => x.getPlans(app))
                .returns(() => of(versioned(version, oldPlans))).verifiable();

            plansState.load(false, 'id2_yearly').subscribe();

            expect(plansState.snapshot.plans).toEqual([
                { isSelected: false, isYearlySelected: false, plan: oldPlans.plans[0] },
                { isSelected: false, isYearlySelected: true,  plan: oldPlans.plans[1] }
            ]);
            expect(plansState.snapshot.hasPortal).toBeTruthy();
            expect(plansState.snapshot.isLoaded).toBeTruthy();
            expect(plansState.snapshot.isLoading).toBeFalsy();
            expect(plansState.snapshot.isOwner).toBeFalsy();
            expect(plansState.snapshot.version).toEqual(version);

            dialogs.verify(x => x.notifyInfo(It.isAnyString()), Times.never());
        });

        it('should reset loading state if loading failed', () => {
            plansService.setup(x => x.getPlans(app))
                .returns(() => throwError('Service Error'));

            plansState.load().pipe(onErrorResumeNext()).subscribe();

            expect(plansState.snapshot.isLoading).toBeFalsy();
        });

        it('should show notification on load if reload is true', () => {
            plansService.setup(x => x.getPlans(app))
                .returns(() => of(versioned(version, oldPlans))).verifiable();

            plansState.load(true).subscribe();

            expect().nothing();

            dialogs.verify(x => x.notifyInfo(It.isAnyString()), Times.once());
        });
    });

    describe('Updates', () => {
        beforeEach(() => {
            plansState.window = <any>{ location: {} };

            plansService.setup(x => x.getPlans(app))
                .returns(() => of(versioned(version, oldPlans))).verifiable();

            plansState.load().subscribe();
        });

        it('should redirect if returning url', () => {
            plansState.window = <any>{ location: {} };

            const result = { redirectUri: 'http://url' };

            plansService.setup(x => x.putPlan(app, It.isAny(), version))
                .returns(() => of(versioned(newVersion, result)));

            plansState.change('free').pipe(onErrorResumeNext()).subscribe();

            expect(plansState.snapshot.plans).toEqual([
                { isSelected: true,  isYearlySelected: false, plan: oldPlans.plans[0] },
                { isSelected: false, isYearlySelected: false, plan: oldPlans.plans[1] }
            ]);
            expect(plansState.window.location.href).toBe(result.redirectUri);
            expect(plansState.snapshot.version).toEqual(version);
        });

        it('should update plans if no returning url', () => {
            plansService.setup(x => x.putPlan(app, It.isAny(), version))
                .returns(() => of(versioned(newVersion, { redirectUri: '' })));

            plansState.change('id2_yearly').pipe(onErrorResumeNext()).subscribe();

            expect(plansState.snapshot.plans).toEqual([
                { isSelected: false, isYearlySelected: false, plan: oldPlans.plans[0] },
                { isSelected: false, isYearlySelected: true,  plan: oldPlans.plans[1] }
            ]);
            expect(plansState.snapshot.isOwner).toBeTruthy();
            expect(plansState.snapshot.version).toEqual(newVersion);
        });
    });
});