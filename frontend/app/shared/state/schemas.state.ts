/*
 * Squidex Headless CMS
 *
 * @license
 * Copyright (c) Squidex UG (haftungsbeschränkt). All rights reserved.
 */

import { Injectable } from '@angular/core';
import { compareStrings, DialogService, shareMapSubscribed, shareSubscribed, State, Version } from '@app/framework';
import { EMPTY, Observable, of } from 'rxjs';
import { catchError, finalize, tap } from 'rxjs/operators';
import { AddFieldDto, CreateSchemaDto, FieldDto, FieldRule, NestedFieldDto, RootFieldDto, SchemaDto, SchemasService, UpdateFieldDto, UpdateSchemaDto, UpdateUIFields } from './../services/schemas.service';
import { AppsState } from './apps.state';

type AnyFieldDto = NestedFieldDto | RootFieldDto;

interface Snapshot {
    // The schema categories.
    categories: ReadonlyArray<string>;

    // The current schemas.
    schemas: SchemasList;

    // Indicates if the schemas are loaded.
    isLoaded?: boolean;

    // Indicates if the schemas are loading.
    isLoading?: boolean;

    // The selected schema.
    selectedSchema?: SchemaDto | null;

    // Indicates if the user can create a schema.
    canCreate?: boolean;
}

export type SchemasList = ReadonlyArray<SchemaDto>;
export type SchemaCategory = { name: string; schemas: SchemasList; upper: string; };

@Injectable()
export class SchemasState extends State<Snapshot> {
    public categoryNames =
        this.project(x => x.categories);

    public selectedSchema =
        this.project(x => x.selectedSchema);

    public schemas =
        this.project(x => x.schemas);

    public isLoaded =
        this.project(x => x.isLoaded === true);

    public isLoading =
        this.project(x => x.isLoading === true);

    public canCreate =
        this.project(x => x.canCreate === true);

    public publishedSchemas =
        this.projectFrom(this.schemas, x => x.filter(s => s.isPublished));

    public categories =
        this.projectFrom2(this.schemas, this.categoryNames, (s, c) => buildCategories(c, s));

    public get schemaId() {
        return this.snapshot.selectedSchema?.id || '';
    }

    public get schemaName() {
        return this.snapshot.selectedSchema?.name || '';
    }

    constructor(
        private readonly appsState: AppsState,
        private readonly dialogs: DialogService,
        private readonly schemasService: SchemasService
    ) {
        super({ schemas: [], categories: [] }, 'Schemas');
    }

    public select(idOrName: string | null): Observable<SchemaDto | null> {
        return this.loadSchema(idOrName).pipe(
            tap(selectedSchema => {
                this.next({ selectedSchema }, 'Selected');
            }));
    }

    public loadSchema(idOrName: string | null, cached = false) {
        if (!idOrName) {
            return of(null);
        }

        const found = this.snapshot.schemas.find(x => x.id === idOrName || x.name === idOrName);

        if (cached || !found) {
            return of(found || null);
        }

        return this.schemasService.getSchema(this.appName, idOrName).pipe(
            tap(schema => {
                this.next(s => {
                    const schemas = s.schemas.replacedBy('id', schema);

                    return { ...s, schemas };
                }, 'Loading Schema Done');
            }),
            catchError(() => of(null)));
    }

    public load(isReload = false): Observable<any> {
        return this.loadInternal(isReload);
    }

    public loadIfNotLoaded(): Observable<any> {
        if (this.snapshot.isLoaded) {
            return EMPTY;
        }

        return this.loadInternal(false);
    }

    private loadInternal(isReload: boolean): Observable<any> {
        this.next({ isLoading: true }, 'Loading Started');

        return this.schemasService.getSchemas(this.appName).pipe(
            tap(({ items, canCreate }) => {
                if (isReload) {
                    this.dialogs.notifyInfo('i18n:schemas.reloaded');
                }

                const schemas = items.sortedByString(x => x.displayName);

                return this.next({
                    canCreate,
                    isLoaded: true,
                    isLoading: true,
                    schemas
                }, 'Loading Success');
            }),
            finalize(() => {
                this.next({ isLoading: false }, 'Loading Done');
            }),
            shareSubscribed(this.dialogs));
    }

    public create(request: CreateSchemaDto): Observable<SchemaDto> {
        return this.schemasService.postSchema(this.appName, request).pipe(
            tap(created => {
                this.next(s => {
                    const schemas = [...s.schemas, created].sortByString(x => x.displayName);

                    return { ...s, schemas };
                }, 'Created');
            }),
            shareSubscribed(this.dialogs, { silent: true }));
    }

    public delete(schema: SchemaDto): Observable<any> {
        return this.schemasService.deleteSchema(this.appName, schema, schema.version).pipe(
            tap(() => {
                this.next(s => {
                    const schemas = s.schemas.filter(x => x.id !== schema.id);

                    const selectedSchema =
                        s.selectedSchema?.id !== schema.id ?
                        s.selectedSchema :
                        null;

                    return { ...s, schemas, selectedSchema };
                }, 'Deleted');
            }),
            shareSubscribed(this.dialogs));
    }

    public addCategory(name: string) {
        this.next(s => {
            const categories = [...s.categories, name];

            return { ...s, categories };
        }, 'Category Added');
    }

    public removeCategory(name: string) {
        this.next(s => {
            const categories = s.categories.removed(name);

            return { ...s, categories };
        }, 'Category Removed');
    }

    public publish(schema: SchemaDto): Observable<SchemaDto> {
        return this.schemasService.publishSchema(this.appName, schema, schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated);
            }),
            shareSubscribed(this.dialogs));
    }

    public unpublish(schema: SchemaDto): Observable<SchemaDto> {
        return this.schemasService.unpublishSchema(this.appName, schema, schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated);
            }),
            shareSubscribed(this.dialogs));
    }

    public changeCategory(schema: SchemaDto, name: string): Observable<SchemaDto> {
        return this.schemasService.putCategory(this.appName, schema, { name }, schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated);
            }),
            shareSubscribed(this.dialogs));
    }

    public configurePreviewUrls(schema: SchemaDto, request: {}): Observable<SchemaDto> {
        return this.schemasService.putPreviewUrls(this.appName, schema, request, schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated, schema.version, 'i18n:schemas.saved');
            }),
            shareSubscribed(this.dialogs));
    }

    public configureFieldRules(schema: SchemaDto, request: ReadonlyArray<FieldRule>): Observable<SchemaDto> {
        return this.schemasService.putFieldRules(this.appName, schema, request, schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated, schema.version, 'i18n:schemas.saved');
            }),
            shareSubscribed(this.dialogs));
    }

    public configureScripts(schema: SchemaDto, request: {}): Observable<SchemaDto> {
        return this.schemasService.putScripts(this.appName, schema, request, schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated, schema.version, 'i18n:schemas.saved');
            }),
            shareSubscribed(this.dialogs));
    }

    public synchronize(schema: SchemaDto, request: {}): Observable<SchemaDto> {
        return this.schemasService.putSchemaSync(this.appName, schema, request, schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated, schema.version, 'i18n:schemas.synchronized');
            }),
            shareSubscribed(this.dialogs));
    }

    public update(schema: SchemaDto, request: UpdateSchemaDto): Observable<SchemaDto> {
        return this.schemasService.putSchema(this.appName, schema, request, schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated, schema.version, 'i18n:schemas.saved');
            }),
            shareSubscribed(this.dialogs));
    }

    public addField(schema: SchemaDto, request: AddFieldDto, parent?: RootFieldDto): Observable<FieldDto> {
        return this.schemasService.postField(this.appName, parent || schema, request, schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated);
            }),
            shareMapSubscribed(this.dialogs, x => getField(x, request, parent), { silent: true }));
    }

    public configureUIFields(schema: SchemaDto, request: UpdateUIFields): Observable<SchemaDto> {
        return this.schemasService.putUIFields(this.appName, schema, request, schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated, schema.version, 'i18n:schemas.saved');
            }),
            shareSubscribed(this.dialogs));
    }

    public orderFields(schema: SchemaDto, fields: ReadonlyArray<any>, parent?: RootFieldDto): Observable<SchemaDto> {
        return this.schemasService.putFieldOrdering(this.appName, parent || schema, fields.map(t => t.fieldId), schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated, schema.version, 'i18n:schemas.saved');
            }),
            shareSubscribed(this.dialogs));
    }

    public lockField<T extends FieldDto>(schema: SchemaDto, field: T): Observable<SchemaDto> {
        return this.schemasService.lockField(this.appName, field, schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated);
            }),
            shareSubscribed(this.dialogs));
    }

    public enableField<T extends FieldDto>(schema: SchemaDto, field: T): Observable<SchemaDto> {
        return this.schemasService.enableField(this.appName, field, schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated);
            }),
            shareSubscribed(this.dialogs));
    }

    public disableField<T extends FieldDto>(schema: SchemaDto, field: T): Observable<SchemaDto> {
        return this.schemasService.disableField(this.appName, field, schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated);
            }),
            shareSubscribed(this.dialogs));
    }

    public showField<T extends FieldDto>(schema: SchemaDto, field: T): Observable<SchemaDto> {
        return this.schemasService.showField(this.appName, field, schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated);
            }),
            shareSubscribed(this.dialogs));
    }

    public hideField<T extends FieldDto>(schema: SchemaDto, field: T): Observable<SchemaDto> {
        return this.schemasService.hideField(this.appName, field, schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated);
            }),
            shareSubscribed(this.dialogs));
    }

    public updateField<T extends FieldDto>(schema: SchemaDto, field: T, request: UpdateFieldDto): Observable<SchemaDto> {
        return this.schemasService.putField(this.appName, field, request, schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated, schema.version, 'i18n:schemas.saved');
            }),
            shareSubscribed(this.dialogs));
    }

    public deleteField(schema: SchemaDto, field: AnyFieldDto): Observable<SchemaDto> {
        return this.schemasService.deleteField(this.appName, field, schema.version).pipe(
            tap(updated => {
                this.replaceSchema(updated);
            }),
            shareSubscribed(this.dialogs));
    }

    private replaceSchema(schema: SchemaDto, oldVersion?: Version, updateText?: string) {
        if (!oldVersion || !oldVersion.eq(schema.version)) {
            if (updateText) {
                this.dialogs.notifyInfo(updateText);
            }

            this.next(s => {
                const schemas = s.schemas.replacedBy('id', schema).sortedByString(x => x.displayName);

                const selectedSchema =
                    schema &&
                    s.selectedSchema &&
                    s.selectedSchema.id === schema.id ?
                    schema :
                    s.selectedSchema;

                return { ...s, schemas, selectedSchema };
            }, 'Updated');
        } else {
            if (updateText) {
                this.dialogs.notifyInfo('i18n:common.nothingChanged');
            }
        }
    }

    private get appName() {
        return this.appsState.appName;
    }
}

function getField(x: SchemaDto, request: AddFieldDto, parent?: RootFieldDto): FieldDto {
    if (parent) {
        return x.fields.find(f => f.fieldId === parent.fieldId)!.nested.find(f => f.name === request.name)!;
    } else {
        return x.fields.find(f => f.name === request.name)!;
    }
}

const NO_NAME = '';

function buildCategories(categories: ReadonlyArray<string>, allSchemas: SchemasList): ReadonlyArray<SchemaCategory> {
    const uniqueCategories: { [name: string]: true } = {
        [NO_NAME]: true
    };

    for (const category of categories) {
        uniqueCategories[category] = true;
    }

    for (const schema of allSchemas) {
        uniqueCategories[getCategory(schema)] = true;
    }

    const result: SchemaCategory[] = [];

    for (const name of Object.keys(uniqueCategories)) {
        const schemas = allSchemas.filter(x => isSameCategory(name, x));

        result.push({ name, upper: name.toUpperCase(), schemas });
    }

    result.sort((a, b) => compareStrings(a.upper, b.upper));

    return result;
}

function getCategory(schema: SchemaDto) {
    return schema.category || NO_NAME;
}

export function isSameCategory(name: string, schema: SchemaDto): boolean {
    return getCategory(schema) === name;
}